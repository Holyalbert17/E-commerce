using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Sync;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Datamex.ECommerce.Infrastructure.Sync;

public sealed class PedidoErpMigrationService : IPedidoErpMigrationService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IConfiguration _config;
    private readonly ICuentaErpWriter _cuentaErp;
    private readonly IErpConnectionResolver _erpCs;

    public PedidoErpMigrationService(
        EcomDbContext db,
        ITenantContext tenant,
        IConfiguration config,
        ICuentaErpWriter cuentaErp,
        IErpConnectionResolver erpCs)
    {
        _db = db;
        _tenant = tenant;
        _config = config;
        _cuentaErp = cuentaErp;
        _erpCs = erpCs;
    }

    public async Task<MigrarPedidosResult> MigrarAsync(IReadOnlyList<long> ids, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        if (ids is null || ids.Count == 0)
            throw new ArgumentException("Ids vacío.");

        var set = ids.Distinct().ToList();
        var pedidos = await _db.PedidosMovil
            .Include(p => p.DetallePedidoMoviles)
            .Where(p => p.IDEmpresa == idEmpresa
                        && set.Contains(p.ID)
                        && p.OrigenPedido == OrigenPedidoMovil.Ecommerce)
            .ToListAsync(ct);

        var config = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);
        var procesaErp = (config?.MigrarPedidosAlGenerar ?? false) || (config?.ProcesaPedidosERP ?? false);
        var erpCs = await _erpCs.ResolveAsync(idEmpresa, ct);
        var allowWriteBack = await _erpCs.AllowWriteBackAsync(idEmpresa, ct);

        var logs = new List<MigracionPedidoLogDto>();
        var exitosos = 0;
        var errores = 0;
        var now = DateTime.UtcNow;

        foreach (var pedido in pedidos)
        {
            // Ya migrado localmente: solo re-linkea / confirma idempotencia, no re-INSERT.
            if (pedido.Migrado)
            {
                var prevOk = await _db.MigracionesPedidoLog.AsNoTracking()
                    .Where(l => l.IDEmpresa == idEmpresa && l.IDPedidoMovil == pedido.ID && l.Estado == "ok")
                    .OrderByDescending(l => l.FechaIntento)
                    .FirstOrDefaultAsync(ct);
                logs.Add(new MigracionPedidoLogDto(
                    prevOk?.ID ?? 0, pedido.ID, null, pedido.NroPedido, "ok",
                    null,
                    prevOk?.DetalleTecnico ?? "Ya migrado localmente (skip).",
                    prevOk?.IDPedidoMovilErp ?? pedido.ID,
                    prevOk?.Intentos ?? 0, prevOk?.FechaIntento ?? now, prevOk?.UserNameOperador));
                exitosos++;
                continue;
            }

            var cart = await _db.CarritosCompra
                .Where(c => c.IDEmpresa == idEmpresa && c.IDPedidoMovil == pedido.ID)
                .OrderByDescending(c => c.ID)
                .FirstOrDefaultAsync(ct);

            var intentos = await _db.MigracionesPedidoLog
                .CountAsync(l => l.IDEmpresa == idEmpresa && l.IDPedidoMovil == pedido.ID, ct);

            var log = new MigracionPedidoLog
            {
                IDEmpresa = idEmpresa,
                Activo = true,
                UltModificacion = now,
                UserName = "admin-migracion",
                IDPedidoMovil = pedido.ID,
                IDCarritoCompra = cart?.ID,
                NroPedido = pedido.NroPedido,
                Estado = "pendiente",
                Intentos = intentos + 1,
                FechaIntento = now,
                UserNameOperador = "admin"
            };
            _db.MigracionesPedidoLog.Add(log);

            try
            {
                if (!procesaErp)
                {
                    log.Estado = "error";
                    log.MensajeError = "Empresa no procesa pedidos ERP (ProcesaPedidosERP/MigrarPedidosAlGenerar=false).";
                    pedido.MotivoPendiente = Trunc("migración ERP pendiente: MigrarPedidosAlGenerar=false", 512);
                    errores++;
                }
                else if (string.IsNullOrWhiteSpace(erpCs) || !allowWriteBack)
                {
                    // No marcar Migrado: el pedido queda pendiente de INSERT real (sin ETL).
                    log.Estado = "skipped";
                    log.DetalleTecnico = string.IsNullOrWhiteSpace(erpCs)
                        ? "Sin ConnectionStrings:Erp — pedido pendiente de write-back."
                        : "Erp:AllowWriteBack=false — no se INSERTA en ERP (pedido pendiente).";
                    pedido.MotivoPendiente = Trunc(
                        string.IsNullOrWhiteSpace(erpCs)
                            ? "migración ERP pendiente: sin connection string"
                            : "migración ERP pendiente: AllowWriteBack=false",
                        512);
                    // Contamos como "procesado sin error de negocio"; reintento lo completa.
                    exitosos++;
                }
                else
                {
                    // Paridad ERP: Cliente+Cuenta existen antes del PedidoMovil (activación o acá).
                    var erpCuentaId = await _cuentaErp.EnsureCuentaInErpByIdAsync(
                        idEmpresa, pedido.IDCuenta, ct);
                    if (erpCuentaId is null or <= 0)
                        throw new InvalidOperationException(
                            $"No se pudo asegurar cuenta ERP (IDCuenta local={pedido.IDCuenta}).");

                    var erpId = await WriteToErpAsync(_db, pedido, erpCs, erpCuentaId.Value, ct);
                    log.IDPedidoMovilErp = erpId;
                    log.DetalleTecnico =
                        $"UPSERT PedidoMovil ERP id={erpId} cuentaErp={erpCuentaId} (idempotente por ECO-PM:{pedido.ID})";
                    await MarkSuccessAsync(pedido, cart, log, now, ct);
                    pedido.MotivoPendiente = null;
                    exitosos++;
                }
            }
            catch (Exception ex)
            {
                log.Estado = "error";
                log.MensajeError = Trunc(ex.Message, 500);
                log.DetalleTecnico = Trunc(ex.ToString(), 2000);
                pedido.MotivoPendiente = Trunc($"migración ERP pendiente: {ex.Message}", 512);
                // Nunca marcar Migrado=true en error — permite reintento sin perder el pedido.
                errores++;
            }

            await _db.SaveChangesAsync(ct);
            logs.Add(MapLog(log));
        }

        return new MigrarPedidosResult(pedidos.Count, exitosos, errores, logs);
    }

    public async Task<IReadOnlyList<MigracionPedidoLogDto>> ListLogsAsync(
        long? idPedidoMovil = null, int limit = 100, CancellationToken ct = default)
    {
        var idEmpresa = RequireTenant();
        limit = Math.Clamp(limit, 1, 500);

        var q = _db.MigracionesPedidoLog.AsNoTracking()
            .Where(l => l.IDEmpresa == idEmpresa);

        if (idPedidoMovil.HasValue)
            q = q.Where(l => l.IDPedidoMovil == idPedidoMovil.Value);

        return await q
            .OrderByDescending(l => l.FechaIntento)
            .ThenByDescending(l => l.ID)
            .Take(limit)
            .Select(l => new MigracionPedidoLogDto(
                l.ID, l.IDPedidoMovil, l.IDCarritoCompra, l.NroPedido, l.Estado,
                l.MensajeError, l.DetalleTecnico, l.IDPedidoMovilErp, l.Intentos,
                l.FechaIntento, l.UserNameOperador))
            .ToListAsync(ct);
    }

    /// <summary>
    /// INSERT idempotente en Postgres ERP (dbo.pedidomovil / dbo.detallepedidomovil).
    /// Idempotencia por tag [ECO-PM:{idLocal}] en observacion (no solo por nropedido).
    /// </summary>
    private static async Task<long> WriteToErpAsync(
        EcomDbContext db, PedidoMovil pedido, string erpCs, long erpCuentaId, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(erpCs);
        await conn.OpenAsync(ct);
        await using (var setPath = new NpgsqlCommand("SET search_path TO dbo, public", conn))
            await setPath.ExecuteNonQueryAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        // Idempotencia por pedido local (no por N°): un N° duplicado no debe reutilizar otro PedidoMovil ERP.
        var ecoTag = $"[ECO-PM:{pedido.ID}]";
        var observacionErp = string.IsNullOrWhiteSpace(pedido.Observacion)
            ? ecoTag
            : $"{pedido.Observacion.Trim()} {ecoTag}";
        await using (var find = new NpgsqlCommand(
                         """
                         SELECT id FROM dbo.pedidomovil
                         WHERE idempresa = @e AND origenpedido = @o
                           AND observacion LIKE '%' || @tag || '%'
                         LIMIT 1
                         """, conn, tx))
        {
            find.Parameters.AddWithValue("e", pedido.IDEmpresa);
            find.Parameters.AddWithValue("o", OrigenPedidoMovil.Ecommerce);
            find.Parameters.AddWithValue("tag", ecoTag);
            var existing = await find.ExecuteScalarAsync(ct);
            if (existing is not null and not DBNull)
            {
                await tx.CommitAsync(ct);
                return Convert.ToInt64(existing);
            }
        }

        // Si el N° ya existe en ERP para otro pedido e-com, reservar un N° libre.
        await using (var clash = new NpgsqlCommand(
                         """
                         SELECT 1 FROM dbo.pedidomovil
                         WHERE idempresa = @e AND nropedido = @n AND origenpedido = @o
                         LIMIT 1
                         """, conn, tx))
        {
            clash.Parameters.AddWithValue("e", pedido.IDEmpresa);
            clash.Parameters.AddWithValue("n", pedido.NroPedido);
            clash.Parameters.AddWithValue("o", OrigenPedidoMovil.Ecommerce);
            var hit = await clash.ExecuteScalarAsync(ct);
            if (hit is not null and not DBNull)
            {
                await using var next = new NpgsqlCommand(
                    """
                    SELECT COALESCE(MAX(nropedido), 0) + 1
                    FROM dbo.pedidomovil
                    WHERE idempresa = @e AND origenpedido = @o
                    """, conn, tx);
                next.Parameters.AddWithValue("e", pedido.IDEmpresa);
                var n = Convert.ToInt64(await next.ExecuteScalarAsync(ct));
                pedido.NroPedido = n;
                pedido.NroPedidoDispositivoMovil = n;
            }
        }

        // Resolver FKs desde la cuenta ERP (paridad CarritoCompraService: vendedor/condición de la cuenta).
        long idVendedor;
        long idCondicionVenta;
        long idDeposito = pedido.IDDeposito;
        long idCotizacion = pedido.IDCotizacionComercial;
        long idUsuario = pedido.IDUsuario > 0 ? pedido.IDUsuario : 1;
        await using (var fkCmd = new NpgsqlCommand(
                         """
                         SELECT c.idvendedor, c.idcondicionventa
                         FROM dbo.cuenta c
                         WHERE c.id = @id
                         LIMIT 1
                         """, conn, tx))
        {
            fkCmd.Parameters.AddWithValue("id", erpCuentaId);
            await using var fk = await fkCmd.ExecuteReaderAsync(ct);
            if (!await fk.ReadAsync(ct))
                throw new InvalidOperationException($"Cuenta ERP {erpCuentaId} no encontrada al migrar pedido.");
            idVendedor = fk.GetInt64(0);
            idCondicionVenta = fk.GetInt64(1);
        }

        // Validar vendedor; fallback al primer vendedor activo de la empresa.
        await using (var chkVend = new NpgsqlCommand(
                         "SELECT 1 FROM dbo.vendedor WHERE id = @id AND activo LIMIT 1", conn, tx))
        {
            chkVend.Parameters.AddWithValue("id", idVendedor);
            var ok = await chkVend.ExecuteScalarAsync(ct);
            if (ok is null or DBNull)
            {
                await using var anyVend = new NpgsqlCommand(
                    """
                    SELECT id FROM dbo.vendedor
                    WHERE idempresa = @e AND activo
                    ORDER BY id LIMIT 1
                    """, conn, tx);
                anyVend.Parameters.AddWithValue("e", pedido.IDEmpresa);
                var v = await anyVend.ExecuteScalarAsync(ct);
                if (v is null or DBNull)
                    throw new InvalidOperationException("No hay vendedor activo en ERP para la empresa.");
                idVendedor = Convert.ToInt64(v);
            }
        }

        // Depósito: preferir el del pedido si existe en ERP; si no, depósito de la agencia de la cuenta
        // (Casa Matriz / salón) antes que un depósito arbitrario.
        await using (var chkDep = new NpgsqlCommand(
                         "SELECT 1 FROM dbo.deposito WHERE id = @id AND activo LIMIT 1", conn, tx))
        {
            chkDep.Parameters.AddWithValue("id", idDeposito);
            var ok = await chkDep.ExecuteScalarAsync(ct);
            if (ok is null or DBNull)
            {
                object? d = null;
                await using (var depAg = new NpgsqlCommand(
                                 """
                                 SELECT d.id
                                 FROM dbo.deposito d
                                 JOIN dbo.cuenta c ON c.idagencia = d.idagencia
                                 WHERE c.id = @cuenta AND d.idempresa = @e AND d.activo
                                 ORDER BY
                                   CASE WHEN upper(d.descripciondeposito) LIKE '%E-COMMERCE%' THEN 0
                                        WHEN upper(d.descripciondeposito) LIKE '%SALON%' THEN 1
                                        ELSE 2 END,
                                   d.coddeposito
                                 LIMIT 1
                                 """, conn, tx))
                {
                    depAg.Parameters.AddWithValue("cuenta", erpCuentaId);
                    depAg.Parameters.AddWithValue("e", pedido.IDEmpresa);
                    d = await depAg.ExecuteScalarAsync(ct);
                }

                if (d is null or DBNull)
                {
                    await using var anyDep = new NpgsqlCommand(
                        """
                        SELECT d.id FROM dbo.deposito d
                        WHERE d.idempresa = @e AND d.activo
                        ORDER BY d.id LIMIT 1
                        """, conn, tx);
                    anyDep.Parameters.AddWithValue("e", pedido.IDEmpresa);
                    d = await anyDep.ExecuteScalarAsync(ct);
                }

                if (d is null or DBNull)
                    throw new InvalidOperationException("No hay depósito activo en ERP para la empresa.");
                idDeposito = Convert.ToInt64(d);
            }
        }

        // Cotización: siempre resolver por empresa + moneda de la lista + fecha del pedido
        // (ERP GetCotizacion exige FechaCambio exacta). No reutilizar cotizaciones de otra empresa.
        {
            var fechaCot = pedido.FechaPedido.Date;
            long idMonedaEmp;
            await using (var monCmd = new NpgsqlCommand(
                             """
                             SELECT COALESCE(lp.idmoneda, em.id)
                             FROM dbo.cuenta c
                             LEFT JOIN dbo.listaprecio lp ON lp.id = c.idlistaprecio
                             LEFT JOIN dbo.empresamoneda em ON em.idempresa = c.idempresa
                               AND em.activo AND em.esdefacturacion
                             WHERE c.id = @cuenta
                             ORDER BY CASE WHEN lp.idmoneda IS NOT NULL THEN 0 ELSE 1 END,
                                      em.id
                             LIMIT 1
                             """, conn, tx))
            {
                monCmd.Parameters.AddWithValue("cuenta", erpCuentaId);
                var mon = await monCmd.ExecuteScalarAsync(ct);
                if (mon is null or DBNull)
                    throw new InvalidOperationException("No hay moneda de facturación en ERP para la cuenta/lista.");
                idMonedaEmp = Convert.ToInt64(mon);
            }

            await using (var cotCmd = new NpgsqlCommand(
                             """
                             SELECT id, montocambio
                             FROM dbo.cotizacioncomercial
                             WHERE idempresa = @e AND idmoneda = @m AND activo
                               AND fechacambio = @f
                             LIMIT 1
                             """, conn, tx))
            {
                cotCmd.Parameters.AddWithValue("e", pedido.IDEmpresa);
                cotCmd.Parameters.AddWithValue("m", idMonedaEmp);
                cotCmd.Parameters.AddWithValue("f", fechaCot);
                await using var cotR = await cotCmd.ExecuteReaderAsync(ct);
                if (await cotR.ReadAsync(ct))
                {
                    idCotizacion = cotR.GetInt64(0);
                    // MontoCambio del pedido debe coincidir con la cotización del día.
                    pedido.MontoCambio = cotR.GetDecimal(1);
                }
                else
                {
                    await cotR.CloseAsync();
                    // Crear cotización del día (Guaraníes=1; otras monedas: última conocida).
                    decimal monto = 1m;
                    await using (var prev = new NpgsqlCommand(
                                     """
                                     SELECT montocambio FROM dbo.cotizacioncomercial
                                     WHERE idempresa = @e AND idmoneda = @m AND activo
                                       AND fechacambio < @f
                                     ORDER BY fechacambio DESC
                                     LIMIT 1
                                     """, conn, tx))
                    {
                        prev.Parameters.AddWithValue("e", pedido.IDEmpresa);
                        prev.Parameters.AddWithValue("m", idMonedaEmp);
                        prev.Parameters.AddWithValue("f", fechaCot);
                        var p = await prev.ExecuteScalarAsync(ct);
                        if (p is not null and not DBNull)
                            monto = Convert.ToDecimal(p);
                    }

                    await using var insCot = new NpgsqlCommand(
                        """
                        INSERT INTO dbo.cotizacioncomercial (
                          idempresa, fechacambio, montocambio, idmoneda, activo, ultmodificacion, username
                        ) VALUES (
                          @e, @f, @monto, @m, true, @ult, 'ecom-migracion'
                        )
                        RETURNING id
                        """, conn, tx);
                    insCot.Parameters.AddWithValue("e", pedido.IDEmpresa);
                    insCot.Parameters.AddWithValue("f", fechaCot);
                    insCot.Parameters.AddWithValue("monto", monto);
                    insCot.Parameters.AddWithValue("m", idMonedaEmp);
                    insCot.Parameters.AddWithValue("ult", DateTime.UtcNow);
                    idCotizacion = Convert.ToInt64(await insCot.ExecuteScalarAsync(ct));
                    pedido.MontoCambio = monto;
                }
            }
        }

        // ListaPrecioCuenta: asegurar asignación ERP cuenta↔lista (Import/Pedido la requieren).
        long? idListaPrecioCuentaErp = null;
        {
            long? idLista = pedido.IDListaPrecioCuenta; // puede venir como id local de asignación o lista
            // Preferir lista de la cuenta ERP.
            await using (var listaCmd = new NpgsqlCommand(
                             "SELECT idlistaprecio FROM dbo.cuenta WHERE id = @id LIMIT 1", conn, tx))
            {
                listaCmd.Parameters.AddWithValue("id", erpCuentaId);
                var l = await listaCmd.ExecuteScalarAsync(ct);
                if (l is not null and not DBNull)
                    idLista = Convert.ToInt64(l);
            }

            // Si no hay, lista ecommerce de la empresa.
            if (idLista is null or <= 0)
            {
                await using var ecomLista = new NpgsqlCommand(
                    """
                    SELECT id FROM dbo.listaprecio
                    WHERE idempresa = @e AND activo
                    ORDER BY CASE WHEN eslistaprecioecommerce THEN 0 ELSE 1 END, id
                    LIMIT 1
                    """, conn, tx);
                ecomLista.Parameters.AddWithValue("e", pedido.IDEmpresa);
                var el = await ecomLista.ExecuteScalarAsync(ct);
                if (el is not null and not DBNull)
                    idLista = Convert.ToInt64(el);
            }

            if (idLista is > 0)
            {
                // Si el valor era un listapreciocuenta.id válido de ESTA cuenta, reutilizarlo.
                await using (var asOwn = new NpgsqlCommand(
                                 """
                                 SELECT id FROM dbo.listapreciocuenta
                                 WHERE id = @id AND idcuenta = @cuenta AND activo
                                 LIMIT 1
                                 """, conn, tx))
                {
                    asOwn.Parameters.AddWithValue("id", pedido.IDListaPrecioCuenta ?? 0L);
                    asOwn.Parameters.AddWithValue("cuenta", erpCuentaId);
                    var own = await asOwn.ExecuteScalarAsync(ct);
                    if (own is not null and not DBNull)
                        idListaPrecioCuentaErp = Convert.ToInt64(own);
                }

                if (idListaPrecioCuentaErp is null)
                {
                    await using var findLpc = new NpgsqlCommand(
                        """
                        SELECT id FROM dbo.listapreciocuenta
                        WHERE idcuenta = @cuenta AND idlistaprecio = @lista AND activo
                        ORDER BY id DESC LIMIT 1
                        """, conn, tx);
                    findLpc.Parameters.AddWithValue("cuenta", erpCuentaId);
                    findLpc.Parameters.AddWithValue("lista", idLista.Value);
                    var found = await findLpc.ExecuteScalarAsync(ct);
                    if (found is not null and not DBNull)
                    {
                        idListaPrecioCuentaErp = Convert.ToInt64(found);
                    }
                    else
                    {
                        await using var insLpc = new NpgsqlCommand(
                            """
                            INSERT INTO dbo.listapreciocuenta (
                              idempresa, idlistaprecio, idcuenta, activo, ultmodificacion, username
                            ) VALUES (
                              @e, @lista, @cuenta, true, @ult, 'ecom-migracion'
                            )
                            RETURNING id
                            """, conn, tx);
                        insLpc.Parameters.AddWithValue("e", pedido.IDEmpresa);
                        insLpc.Parameters.AddWithValue("lista", idLista.Value);
                        insLpc.Parameters.AddWithValue("cuenta", erpCuentaId);
                        insLpc.Parameters.AddWithValue("ult", DateTime.UtcNow);
                        idListaPrecioCuentaErp = Convert.ToInt64(await insLpc.ExecuteScalarAsync(ct));
                    }
                }
            }
        }

        const string insertPedido = """
            INSERT INTO dbo.pedidomovil (
              activo, ultmodificacion, username, idempresa,
              nropedido, idcuenta, idvendedor, iddeposito, indicecomision,
              idcotizacioncomercial, idcondicionventa, idlistapreciocuenta, idusuario,
              fechapedido, montocambio, fechaemision, plazo, porcentajedescuento,
              tipofactura, tipopedido, tipoprecio, fechaprevistaentrega, estado,
              nropedidodispositivomovil, migrado, momento, host, observacion,
              latitud, longitud, precision, origenpedido, motivopendiente
            ) VALUES (
              @activo, @ultmodificacion, @username, @idempresa,
              @nropedido, @idcuenta, @idvendedor, @iddeposito, @indicecomision,
              @idcotizacioncomercial, @idcondicionventa, @idlistapreciocuenta, @idusuario,
              @fechapedido, @montocambio, @fechaemision, @plazo, @porcentajedescuento,
              @tipofactura, @tipopedido, @tipoprecio, @fechaprevistaentrega, @estado,
              @nropedidodispositivomovil, @migrado, @momento, @host, @observacion,
              @latitud, @longitud, @precision, @origenpedido, @motivopendiente
            ) RETURNING id;
            """;

        long erpPedidoId;
        await using (var cmd = new NpgsqlCommand(insertPedido, conn, tx))
        {
            cmd.Parameters.AddWithValue("activo", true);
            cmd.Parameters.AddWithValue("ultmodificacion", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("username", (object?)pedido.UserName ?? "ecom-migracion");
            cmd.Parameters.AddWithValue("idempresa", pedido.IDEmpresa);
            cmd.Parameters.AddWithValue("nropedido", pedido.NroPedido);
            cmd.Parameters.AddWithValue("idcuenta", erpCuentaId);
            cmd.Parameters.AddWithValue("idvendedor", idVendedor);
            cmd.Parameters.AddWithValue("iddeposito", idDeposito);
            cmd.Parameters.AddWithValue("indicecomision", pedido.IndiceComision);
            cmd.Parameters.AddWithValue("idcotizacioncomercial", idCotizacion);
            cmd.Parameters.AddWithValue("idcondicionventa", idCondicionVenta);
            cmd.Parameters.AddWithValue("idlistapreciocuenta", (object?)idListaPrecioCuentaErp ?? DBNull.Value);
            cmd.Parameters.AddWithValue("idusuario", idUsuario);
            cmd.Parameters.AddWithValue("fechapedido", pedido.FechaPedido);
            cmd.Parameters.AddWithValue("montocambio", pedido.MontoCambio <= 0 ? 1m : pedido.MontoCambio);
            cmd.Parameters.AddWithValue("fechaemision", pedido.FechaEmision);
            cmd.Parameters.AddWithValue("plazo", pedido.Plazo);
            cmd.Parameters.AddWithValue("porcentajedescuento", pedido.PorcentajeDescuento);
            cmd.Parameters.AddWithValue("tipofactura", pedido.TipoFactura);
            cmd.Parameters.AddWithValue("tipopedido", pedido.TipoPedido ?? TipoPedidoErp.Venta);
            cmd.Parameters.AddWithValue("tipoprecio", pedido.TipoPrecio);
            cmd.Parameters.AddWithValue("fechaprevistaentrega", (object?)pedido.FechaPrevistaEntrega ?? DBNull.Value);
            // Paridad FinalizarCarrito ERP: Estado = CERRADO (1).
            cmd.Parameters.AddWithValue("estado", EstadoPedidoMovil.Cerrado);
            cmd.Parameters.AddWithValue("nropedidodispositivomovil", pedido.NroPedidoDispositivoMovil);
            // Paridad ERP ecommerce: PedidoMovil nace Migrado=false (planilla/proceso lo marca).
            // Local e-com usa Migrado=true solo como “ya escrito en ERP”.
            cmd.Parameters.AddWithValue("migrado", false);
            cmd.Parameters.AddWithValue("momento", pedido.Momento);
            cmd.Parameters.AddWithValue("host", (object?)pedido.Host ?? DBNull.Value);
            cmd.Parameters.AddWithValue("observacion", observacionErp);
            cmd.Parameters.AddWithValue("latitud", (object?)pedido.Latitud ?? DBNull.Value);
            cmd.Parameters.AddWithValue("longitud", (object?)pedido.Longitud ?? DBNull.Value);
            cmd.Parameters.AddWithValue("precision", (object?)pedido.Precision ?? DBNull.Value);
            cmd.Parameters.AddWithValue("origenpedido", OrigenPedidoMovil.Ecommerce);
            cmd.Parameters.AddWithValue("motivopendiente", DBNull.Value);

            erpPedidoId = Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
        }

        const string insertDetalle = """
            INSERT INTO dbo.detallepedidomovil (
              activo, ultmodificacion, username,
              idpedidomovil, idarticulo, iddetallelistaprecio, idusuariotraspaso, idusuario,
              codigobarra, montocambio, porcentajedescuento,
              preciobrutoconimpuesto, preciobrutosinimpuesto, precionetoconimpuesto, precionetosinimpuesto,
              cantidadembalada, cantidadfraccionada, traspaso, momento, host, observacion,
              nroorden, diferenciapreciosupermercado, preciolista
            ) VALUES (
              @activo, @ultmodificacion, @username,
              @idpedidomovil, @idarticulo, @iddetallelistaprecio, @idusuariotraspaso, @idusuario,
              @codigobarra, @montocambio, @porcentajedescuento,
              @preciobrutoconimpuesto, @preciobrutosinimpuesto, @precionetoconimpuesto, @precionetosinimpuesto,
              @cantidadembalada, @cantidadfraccionada, @traspaso, @momento, @host, @observacion,
              @nroorden, @diferenciapreciosupermercado, @preciolista
            );
            """;

        var idListaPrecio = await ResolveListaPrecioIdAsync(db, conn, tx, pedido, ct);

        foreach (var d in pedido.DetallePedidoMoviles.Where(x => x.Activo))
        {
            var dlpId = await ResolveErpDetalleListaPrecioIdAsync(
                conn, tx, pedido.IDEmpresa, idListaPrecio, d.IDArticulo, d.IDDetalleListaPrecio, ct);

            await using var dcmd = new NpgsqlCommand(insertDetalle, conn, tx);
            dcmd.Parameters.AddWithValue("activo", true);
            dcmd.Parameters.AddWithValue("ultmodificacion", DateTime.UtcNow);
            dcmd.Parameters.AddWithValue("username", (object?)d.UserName ?? "ecom-migracion");
            dcmd.Parameters.AddWithValue("idpedidomovil", erpPedidoId);
            dcmd.Parameters.AddWithValue("idarticulo", d.IDArticulo);
            dcmd.Parameters.AddWithValue("iddetallelistaprecio", dlpId);
            dcmd.Parameters.AddWithValue("idusuariotraspaso", d.IDUsuarioTraspaso);
            dcmd.Parameters.AddWithValue("idusuario", d.IDUsuario);
            dcmd.Parameters.AddWithValue("codigobarra", (object?)d.CodigoBarra ?? DBNull.Value);
            dcmd.Parameters.AddWithValue("montocambio", d.MontoCambio);
            dcmd.Parameters.AddWithValue("porcentajedescuento", d.PorcentajeDescuento);
            dcmd.Parameters.AddWithValue("preciobrutoconimpuesto", d.PrecioBrutoConImpuesto);
            dcmd.Parameters.AddWithValue("preciobrutosinimpuesto", d.PrecioBrutoSinImpuesto);
            dcmd.Parameters.AddWithValue("precionetoconimpuesto", d.PrecioNetoConImpuesto);
            dcmd.Parameters.AddWithValue("precionetosinimpuesto", d.PrecioNetoSinImpuesto);
            dcmd.Parameters.AddWithValue("cantidadembalada", d.CantidadEmbalada);
            dcmd.Parameters.AddWithValue("cantidadfraccionada", d.CantidadFraccionada);
            dcmd.Parameters.AddWithValue("traspaso", d.Traspaso);
            dcmd.Parameters.AddWithValue("momento", d.Momento);
            dcmd.Parameters.AddWithValue("host", (object?)d.Host ?? DBNull.Value);
            dcmd.Parameters.AddWithValue("observacion", (object?)d.Observacion ?? DBNull.Value);
            dcmd.Parameters.AddWithValue("nroorden", (object?)d.NroOrden ?? DBNull.Value);
            dcmd.Parameters.AddWithValue("diferenciapreciosupermercado", d.DiferenciaPrecioSupermercado);
            dcmd.Parameters.AddWithValue("preciolista", (object?)d.PrecioLista ?? DBNull.Value);
            await dcmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
        return erpPedidoId;
    }

    /// <summary>
    /// IDListaPrecioCuenta es FK a listapreciocuenta; resolve listaprecio.id para detalle.
    /// </summary>
    private static async Task<long?> ResolveListaPrecioIdAsync(
        EcomDbContext db,
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        PedidoMovil pedido,
        CancellationToken ct)
    {
        if (pedido.IDListaPrecioCuenta is long lpcId and > 0)
        {
            var local = await db.ListasPrecioCuenta.AsNoTracking()
                .Where(x => x.ID == lpcId)
                .Select(x => (long?)x.IDListaPrecio)
                .FirstOrDefaultAsync(ct);
            if (local is > 0)
                return local;

            await using var erp = new NpgsqlCommand(
                "SELECT idlistaprecio FROM dbo.listapreciocuenta WHERE id = @id LIMIT 1", conn, tx);
            erp.Parameters.AddWithValue("id", lpcId);
            var found = await erp.ExecuteScalarAsync(ct);
            if (found is not null and not DBNull)
                return Convert.ToInt64(found);
        }

        return await db.Cuentas.AsNoTracking()
            .Where(c => c.ID == pedido.IDCuenta && c.IDEmpresa == pedido.IDEmpresa)
            .Select(c => c.IDListaPrecio)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Resuelve FK real dbo.detallelistaprecio (paridad CarritoCompraService ERP).
    /// </summary>
    private static async Task<long> ResolveErpDetalleListaPrecioIdAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        long idEmpresa,
        long? idLista,
        long idArticulo,
        long preferredId,
        CancellationToken ct)
    {
        // Si el id preferido existe en detallelistaprecio, úsalo.
        if (preferredId > 0)
        {
            await using var check = new NpgsqlCommand(
                "SELECT 1 FROM dbo.detallelistaprecio WHERE id = @id LIMIT 1", conn, tx);
            check.Parameters.AddWithValue("id", preferredId);
            var ok = await check.ExecuteScalarAsync(ct);
            if (ok is not null and not DBNull)
                return preferredId;
        }

        await using var find = new NpgsqlCommand(
            """
            SELECT d.id
            FROM dbo.detallelistaprecio d
            JOIN dbo.articuloembalaje ae ON ae.id = d.idarticuloembalaje
            JOIN dbo.listaprecio l ON l.id = d.idlistaprecio
            WHERE l.idempresa = @e
              AND ae.idarticulo = @art
              AND d.activo = true
              AND (@lista IS NULL OR d.idlistaprecio = @lista OR l.eslistaprecioecommerce = true)
            ORDER BY CASE WHEN d.idlistaprecio = @lista THEN 0 ELSE 1 END, d.id
            LIMIT 1
            """, conn, tx);
        find.Parameters.AddWithValue("e", idEmpresa);
        find.Parameters.AddWithValue("art", idArticulo);
        find.Parameters.AddWithValue("lista", (object?)idLista ?? DBNull.Value);
        var found = await find.ExecuteScalarAsync(ct);
        if (found is not null and not DBNull)
            return Convert.ToInt64(found);

        return preferredId > 0 ? preferredId : 1;
    }

    private static Task MarkSuccessAsync(
        PedidoMovil pedido, CarritoCompra? cart, MigracionPedidoLog log, DateTime now, CancellationToken ct)
    {
        log.Estado = "ok";
        log.NroPedido = pedido.NroPedido;
        log.IDPedidoMovilErp ??= pedido.ID;
        SetMigrado(pedido, cart, now);
        if (cart is not null && cart.NroPedidoCarritoCompra != pedido.NroPedido)
            cart.NroPedidoCarritoCompra = pedido.NroPedido;
        return Task.CompletedTask;
    }

    private static void SetMigrado(PedidoMovil pedido, CarritoCompra? cart, DateTime now)
    {
        pedido.Migrado = true;
        pedido.UltModificacion = now;
        pedido.UserName = "ecom-writeback";
        if (cart is not null && !cart.Migrado)
        {
            cart.Migrado = true;
            cart.UltModificacion = now;
            cart.UserName = "ecom-writeback";
        }
    }

    private static MigracionPedidoLogDto MapLog(MigracionPedidoLog l) =>
        new(l.ID, l.IDPedidoMovil, l.IDCarritoCompra, l.NroPedido, l.Estado,
            l.MensajeError, l.DetalleTecnico, l.IDPedidoMovilErp, l.Intentos,
            l.FechaIntento, l.UserNameOperador);

    private static string Trunc(string? s, int max) =>
        string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s[..max]);

    private long RequireTenant()
    {
        if (!_tenant.IsResolved || !_tenant.IDEmpresa.HasValue)
            throw new InvalidOperationException("Tenant no resuelto.");
        return _tenant.IDEmpresa.Value;
    }
}
