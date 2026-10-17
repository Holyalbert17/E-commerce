using Datamex.ECommerce.Application.Sync;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Datamex.ECommerce.Infrastructure.Sync;

public sealed class ErpPedidoLifecycleWriter : IErpPedidoLifecycleWriter
{
    private readonly EcomDbContext _db;
    private readonly IErpConnectionResolver _erpCs;
    private readonly ILogger<ErpPedidoLifecycleWriter> _logger;

    public ErpPedidoLifecycleWriter(
        EcomDbContext db,
        IErpConnectionResolver erpCs,
        ILogger<ErpPedidoLifecycleWriter> logger)
    {
        _db = db;
        _erpCs = erpCs;
        _logger = logger;
    }

    public async Task<ErpLifecycleResult> AnularPedidoAsync(
        long idEmpresa, long idPedidoMovilLocal, string? motivo, CancellationToken ct = default)
    {
        var allow = await _erpCs.AllowWriteBackAsync(idEmpresa, ct);
        var cs = await _erpCs.ResolveAsync(idEmpresa, ct);
        if (!allow || string.IsNullOrWhiteSpace(cs))
            return new ErpLifecycleResult(false, true, "Sin CS ERP o AllowWriteBack=false — anulación solo local.");

        var local = await _db.PedidosMovil.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ID == idPedidoMovilLocal && p.IDEmpresa == idEmpresa, ct);
        if (local is null)
            return new ErpLifecycleResult(false, false, "Pedido local no encontrado.");

        if (FacturaSerie.EsFiscalErp(local.NroFactura))
            return new ErpLifecycleResult(false, false,
                "El pedido ya tiene factura fiscal ERP. Usá devolución (nota de crédito), no cancelación.");

        var erpPmId = await ResolveErpPedidoMovilIdAsync(idEmpresa, idPedidoMovilLocal, local.NroPedido, cs!, ct);
        if (erpPmId is not > 0)
            return new ErpLifecycleResult(false, true, "No se encontró pedidomovil en ERP (¿aún no migrado?).");

        try
        {
            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync(ct);
            await using (var path = new NpgsqlCommand("SET search_path TO dbo, public", conn))
                await path.ExecuteNonQueryAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(ct);

            var obs = Truncate(
                $"[ECO-CANCEL] {(string.IsNullOrWhiteSpace(motivo) ? "Cancelado e-commerce" : motivo.Trim())}",
                480);

            await using (var cmd = new NpgsqlCommand(
                             """
                             UPDATE dbo.pedidomovil
                             SET estado = @est,
                                 observacion = CASE
                                     WHEN observacion IS NULL OR observacion = '' THEN @obs
                                     ELSE LEFT(observacion || ' | ' || @obs, 500)
                                 END,
                                 ultmodificacion = NOW(),
                                 username = 'ecom-cancel'
                             WHERE id = @id AND idempresa = @e
                             """, conn, tx))
            {
                cmd.Parameters.AddWithValue("est", EstadoPedidoMovil.Anulado);
                cmd.Parameters.AddWithValue("obs", obs);
                cmd.Parameters.AddWithValue("id", erpPmId.Value);
                cmd.Parameters.AddWithValue("e", idEmpresa);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            await RestoreErpStockFromDetalleAsync(conn, tx, idEmpresa, erpPmId.Value, local.IDDeposito, asDevolucion: false, ct);
            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "ERP anulado pedidomovil={ErpPm} local={Local} empresa={Empresa}",
                erpPmId, idPedidoMovilLocal, idEmpresa);

            return new ErpLifecycleResult(true, false, "Pedido anulado en ERP y stock reintegrado.", erpPmId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error anulando pedidomovil ERP local={Local}", idPedidoMovilLocal);
            return new ErpLifecycleResult(false, false, $"Error ERP al anular: {ex.Message}");
        }
    }

    public async Task<ErpLifecycleResult> DevolverPedidoAsync(
        long idEmpresa, long idPedidoMovilLocal, string? motivo, CancellationToken ct = default)
    {
        var allow = await _erpCs.AllowWriteBackAsync(idEmpresa, ct);
        var cs = await _erpCs.ResolveAsync(idEmpresa, ct);
        if (!allow || string.IsNullOrWhiteSpace(cs))
            return new ErpLifecycleResult(false, true, "Sin CS ERP o AllowWriteBack=false — devolución solo ticket e-com.");

        var local = await _db.PedidosMovil.AsNoTracking()
            .Include(p => p.DetallePedidoMoviles)
            .FirstOrDefaultAsync(p => p.ID == idPedidoMovilLocal && p.IDEmpresa == idEmpresa, ct);
        if (local is null)
            return new ErpLifecycleResult(false, false, "Pedido local no encontrado.");

        var erpPmId = await ResolveErpPedidoMovilIdAsync(idEmpresa, idPedidoMovilLocal, local.NroPedido, cs!, ct);
        if (erpPmId is not > 0)
            return new ErpLifecycleResult(false, true, "Pedido no hallado en ERP; se cierra solo en e-commerce.");

        // Refrescar factura desde ERP
        var sync = await SincronizarFacturaAsync(idEmpresa, idPedidoMovilLocal, ct);
        var nroFac = sync.NroFactura ?? local.NroFactura;

        if (!FacturaSerie.EsFiscalErp(nroFac))
        {
            // Sin factura fiscal: anular es suficiente
            var anul = await AnularPedidoAsync(idEmpresa, idPedidoMovilLocal, motivo ?? "Devolución sin factura", ct);
            return anul with
            {
                Mensaje = anul.Ok
                    ? "Sin factura fiscal: pedido anulado en ERP (equivalente a devolución pre-facturación)."
                    : anul.Mensaje
            };
        }

        try
        {
            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync(ct);
            await using (var path = new NpgsqlCommand("SET search_path TO dbo, public", conn))
                await path.ExecuteNonQueryAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(ct);

            var factura = await FindFacturaAsync(conn, tx, idEmpresa, erpPmId.Value, local.NroPedido, ct);
            if (factura is null)
            {
                await tx.RollbackAsync(ct);
                return new ErpLifecycleResult(false, false,
                    $"Hay N° factura {nroFac} local pero no se halló dbo.pedido en ERP. Emití/vincular factura en ERP o sincronizá.");
            }

            var idMotivo = await FindMotivoDevolucionAsync(conn, tx, idEmpresa, ct);
            if (idMotivo is not > 0)
            {
                await tx.RollbackAsync(ct);
                return new ErpLifecycleResult(false, false,
                    "No hay MotivoNDNC tipo DEVOLUCION en ERP. Creá uno en ERP (Notas de crédito).");
            }

            var lines = await LoadFacturaDetalleAsync(conn, tx, factura.IdFactura, ct);
            if (lines.Count == 0)
            {
                await tx.RollbackAsync(ct);
                return new ErpLifecycleResult(false, false, "La factura ERP no tiene líneas activas.");
            }

            var idNc = await InsertNotaCreditoDevolucionAsync(
                conn, tx, idEmpresa, local, factura, idMotivo.Value, lines, motivo, ct);

            await RestoreErpStockFromDetalleAsync(conn, tx, idEmpresa, erpPmId.Value, local.IDDeposito, asDevolucion: true, ct);

            // Marcar pedidomovil anulado / observación
            await using (var cmd = new NpgsqlCommand(
                             """
                             UPDATE dbo.pedidomovil
                             SET observacion = CASE
                                     WHEN observacion IS NULL OR observacion = '' THEN @obs
                                     ELSE LEFT(observacion || ' | ' || @obs, 500)
                                 END,
                                 ultmodificacion = NOW(),
                                 username = 'ecom-devolucion'
                             WHERE id = @id AND idempresa = @e
                             """, conn, tx))
            {
                cmd.Parameters.AddWithValue("obs", Truncate($"[ECO-DEV] NC#{idNc} {motivo}", 480));
                cmd.Parameters.AddWithValue("id", erpPmId.Value);
                cmd.Parameters.AddWithValue("e", idEmpresa);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "ERP NC devolución id={Nc} factura={Fac} pm={Pm}",
                idNc, factura.IdFactura, erpPmId);

            return new ErpLifecycleResult(
                true, false,
                $"Nota de crédito DEVOLUCION #{idNc} creada en ERP (factura {nroFac}).",
                erpPmId, idNc, nroFac);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error devolución ERP local={Local}", idPedidoMovilLocal);
            return new ErpLifecycleResult(false, false, $"Error ERP al devolver: {ex.Message}");
        }
    }

    public async Task<ErpFacturaSyncResult> SincronizarFacturaAsync(
        long idEmpresa, long idPedidoMovilLocal, CancellationToken ct = default)
    {
        var cs = await _erpCs.ResolveAsync(idEmpresa, ct);
        if (string.IsNullOrWhiteSpace(cs))
            return new ErpFacturaSyncResult(false, "Sin conexión ERP configurada.");

        var local = await _db.PedidosMovil
            .FirstOrDefaultAsync(p => p.ID == idPedidoMovilLocal && p.IDEmpresa == idEmpresa, ct);
        if (local is null)
            return new ErpFacturaSyncResult(false, "Pedido local no encontrado.");

        try
        {
            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync(ct);
            await using (var path = new NpgsqlCommand("SET search_path TO dbo, public", conn))
                await path.ExecuteNonQueryAsync(ct);

            var erpPmId = await ResolveErpPedidoMovilIdAsync(idEmpresa, idPedidoMovilLocal, local.NroPedido, cs!, ct)
                          ?? idPedidoMovilLocal;

            long? nro = null;
            long? idPedido = null;

            await using (var tx = await conn.BeginTransactionAsync(ct))
            {
                var fac = await FindFacturaAsync(conn, tx, idEmpresa, erpPmId, local.NroPedido, ct);
                await tx.CommitAsync(ct);
                if (fac is not null)
                {
                    nro = fac.NroFactura;
                    idPedido = fac.IdPedido;
                }
            }

            if (!FacturaSerie.EsFacturado(nro))
            {
                await using (var cmd = new NpgsqlCommand(
                                 """
                                 SELECT p.id, p.nrofactura
                                 FROM dbo.pedido p
                                 WHERE p.idempresa = @e
                                   AND p.idpedidomovil = @pm
                                   AND p.nrofactura IS NOT NULL AND p.nrofactura > 0
                                 ORDER BY p.id DESC
                                 LIMIT 1
                                 """, conn))
                {
                    cmd.Parameters.AddWithValue("e", idEmpresa);
                    cmd.Parameters.AddWithValue("pm", erpPmId);
                    await using var r = await cmd.ExecuteReaderAsync(ct);
                    if (await r.ReadAsync(ct))
                    {
                        idPedido = r.GetInt64(0);
                        nro = r.IsDBNull(1) ? null : Convert.ToInt64(r.GetValue(1));
                    }
                }
            }

            if (!FacturaSerie.EsFacturado(nro))
            {
                await using var cmd = new NpgsqlCommand(
                    """
                    SELECT nrofactura FROM dbo.pedidomovil
                    WHERE idempresa = @e AND id = @pm AND nrofactura IS NOT NULL AND nrofactura > 0
                    LIMIT 1
                    """, conn);
                cmd.Parameters.AddWithValue("e", idEmpresa);
                cmd.Parameters.AddWithValue("pm", erpPmId);
                var scalar = await cmd.ExecuteScalarAsync(ct);
                if (scalar is not null and not DBNull)
                    nro = Convert.ToInt64(scalar);
            }

            if (nro is not > 0)
            {
                if (local.NroFactura is > 0)
                {
                    local.NroFactura = null;
                    local.UltModificacion = DateTime.UtcNow;
                    local.UserName = "erp-factura-clear";
                    await _db.SaveChangesAsync(ct);
                }

                return new ErpFacturaSyncResult(false, "ERP aún no emitió factura para este pedido.");
            }

            if (local.NroFactura != nro)
            {
                local.NroFactura = nro;
                local.UltModificacion = DateTime.UtcNow;
                local.UserName = "erp-factura-sync";
                await _db.SaveChangesAsync(ct);
            }

            var fiscal = FacturaSerie.EsFiscalErp(nro);
            return new ErpFacturaSyncResult(
                true,
                FacturaSerie.EsFacturado(nro)
                    ? $"Factura ERP N° {nro} sincronizada."
                    : $"N° {nro} sincronizado.",
                nro, idPedido, fiscal);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sync factura ERP falló pm={Pm}", idPedidoMovilLocal);
            return new ErpFacturaSyncResult(false, $"Error sync factura: {ex.Message}");
        }
    }

    private async Task<long?> ResolveErpPedidoMovilIdAsync(
        long idEmpresa, long idLocal, long nroPedido, string cs, CancellationToken ct)
    {
        var fromLog = await _db.MigracionesPedidoLog.AsNoTracking()
            .Where(l => l.IDEmpresa == idEmpresa && l.IDPedidoMovil == idLocal && l.Estado == "ok" && l.IDPedidoMovilErp != null)
            .OrderByDescending(l => l.FechaIntento)
            .Select(l => l.IDPedidoMovilErp)
            .FirstOrDefaultAsync(ct);
        if (fromLog is > 0) return fromLog;

        await using var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync(ct);
        await using (var path = new NpgsqlCommand("SET search_path TO dbo, public", conn))
            await path.ExecuteNonQueryAsync(ct);

        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT id FROM dbo.pedidomovil
                         WHERE idempresa = @e AND id = @id
                         LIMIT 1
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            cmd.Parameters.AddWithValue("id", idLocal);
            var s = await cmd.ExecuteScalarAsync(ct);
            if (s is not null and not DBNull) return Convert.ToInt64(s);
        }

        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT id FROM dbo.pedidomovil
                         WHERE idempresa = @e AND nropedido = @nro
                         ORDER BY id DESC LIMIT 1
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            cmd.Parameters.AddWithValue("nro", nroPedido);
            var s = await cmd.ExecuteScalarAsync(ct);
            if (s is not null and not DBNull) return Convert.ToInt64(s);
        }

        // Tag e-com en observación
        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT id FROM dbo.pedidomovil
                         WHERE idempresa = @e AND observacion LIKE @tag
                         ORDER BY id DESC LIMIT 1
                         """, conn))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            cmd.Parameters.AddWithValue("tag", $"%[ECO-PM:{idLocal}]%");
            var s = await cmd.ExecuteScalarAsync(ct);
            if (s is not null and not DBNull) return Convert.ToInt64(s);
        }

        return null;
    }

    private sealed record FacturaRef(long IdFactura, long IdPedido, long IdCuenta, long NroFactura, long IdCotizacion, long IdDeposito);

    private static async Task<FacturaRef?> FindFacturaAsync(
        NpgsqlConnection conn, NpgsqlTransaction tx, long idEmpresa, long erpPmId, long nroPedido, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            """
            SELECT f.id, f.idpedido, f.idcuenta, f.nrofactura,
                   COALESCE(f.idcotizacioncomercial, 1), COALESCE(f.iddeposito, 1)
            FROM dbo.factura f
            INNER JOIN dbo.pedido p ON p.id = f.idpedido
            WHERE f.idempresa = @e
              AND p.idpedidomovil = @pm
              AND f.nrofactura IS NOT NULL AND f.nrofactura > 0
              AND COALESCE(f.activo, true) = true
            ORDER BY f.id DESC
            LIMIT 1
            """, conn, tx);
        cmd.Parameters.AddWithValue("e", idEmpresa);
        cmd.Parameters.AddWithValue("pm", erpPmId);
        cmd.Parameters.AddWithValue("nro", nroPedido);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        return new FacturaRef(
            r.GetInt64(0), r.GetInt64(1), r.GetInt64(2),
            Convert.ToInt64(r.GetValue(3)), r.GetInt64(4), r.GetInt64(5));
    }

    private static async Task<long?> FindMotivoDevolucionAsync(
        NpgsqlConnection conn, NpgsqlTransaction tx, long idEmpresa, CancellationToken ct)
    {
        // tiponotacredito 0 = DEVOLUCION
        await using var cmd = new NpgsqlCommand(
            """
            SELECT id FROM dbo.motivondnc
            WHERE idempresa = @e
              AND COALESCE(activo, true) = true
              AND tiponotacredito = 0
            ORDER BY id
            LIMIT 1
            """, conn, tx);
        cmd.Parameters.AddWithValue("e", idEmpresa);
        var s = await cmd.ExecuteScalarAsync(ct);
        return s is null or DBNull ? null : Convert.ToInt64(s);
    }

    private sealed record FacLine(
        long IdArticulo, long IdArticuloEmbalaje, long IdDetalleListaPrecio, int CantEmb, int CantFrac,
        double PrecioBrutoCon, double PrecioBrutoSin, double PrecioNetoCon, double PrecioNetoSin,
        double PorcDesc, long IdRegimen, double CantXEmb, double PorcImp,
        decimal MontoTotal, decimal MontoImpuesto, decimal MontoExento, decimal MontoGravado);

    private static async Task<List<FacLine>> LoadFacturaDetalleAsync(
        NpgsqlConnection conn, NpgsqlTransaction tx, long idFactura, CancellationToken ct)
    {
        var list = new List<FacLine>();
        // detallefactura (ERP) usa idarticuloembalaje (EntidadDetalleVenta), no idarticulo.
        await using var cmd = new NpgsqlCommand(
            """
            SELECT ae.idarticulo, df.idarticuloembalaje, COALESCE(df.iddetallelistaprecio, 1),
                   COALESCE(df.cantidadembalada, 0), COALESCE(df.cantidadfraccionada, 0),
                   COALESCE(df.preciobrutoconimpuesto, 0), COALESCE(df.preciobrutosinimpuesto, 0),
                   COALESCE(df.precionetoconimpuesto, 0), COALESCE(df.precionetosinimpuesto, 0),
                   COALESCE(df.porcentajedescuento, 0),
                   df.idregimenimpositivo, COALESCE(df.cantidadxembalaje, 1), COALESCE(df.porcentajeimpuesto, 0),
                   COALESCE(df.montototal, 0), COALESCE(df.montoimpuesto, 0),
                   COALESCE(df.montoexento, 0), COALESCE(df.montogravado, 0)
            FROM dbo.detallefactura df
            INNER JOIN dbo.articuloembalaje ae ON ae.id = df.idarticuloembalaje
            WHERE df.idfactura = @f AND COALESCE(df.activo, true) = true
            """, conn, tx);
        cmd.Parameters.AddWithValue("f", idFactura);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            list.Add(new FacLine(
                r.GetInt64(0), r.GetInt64(1), r.GetInt64(2),
                Convert.ToInt32(r.GetValue(3)), Convert.ToInt32(r.GetValue(4)),
                Convert.ToDouble(r.GetValue(5)), Convert.ToDouble(r.GetValue(6)),
                Convert.ToDouble(r.GetValue(7)), Convert.ToDouble(r.GetValue(8)),
                Convert.ToDouble(r.GetValue(9)),
                r.GetInt64(10), Convert.ToDouble(r.GetValue(11)), Convert.ToDouble(r.GetValue(12)),
                Convert.ToDecimal(r.GetValue(13)), Convert.ToDecimal(r.GetValue(14)),
                Convert.ToDecimal(r.GetValue(15)), Convert.ToDecimal(r.GetValue(16))));
        }

        return list;
    }

    private static async Task<long> InsertNotaCreditoDevolucionAsync(
        NpgsqlConnection conn, NpgsqlTransaction tx,
        long idEmpresa, PedidoMovil local, FacturaRef factura, long idMotivo,
        IReadOnlyList<FacLine> lines, string? motivo, CancellationToken ct)
    {
        // Contador empresa
        long nroNc;
        await using (var cmd = new NpgsqlCommand(
                         """
                         UPDATE dbo.empresatransaccion
                         SET nroultnotacredito = COALESCE(nroultnotacredito, 0) + 1
                         WHERE id = @e
                         RETURNING nroultnotacredito
                         """, conn, tx))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            object? s;
            try
            {
                s = await cmd.ExecuteScalarAsync(ct);
            }
            catch (PostgresException ex) when (ex.SqlState is "42P01" or "42703")
            {
                s = null;
            }

            if (s is null or DBNull)
            {
                // Fallback sin contador (tabla/columna ausente o empresa sin fila)
                await using var q = new NpgsqlCommand(
                    "SELECT COALESCE(MAX(nronotacredito),0)+1 FROM dbo.notacredito WHERE idempresa=@e", conn, tx);
                q.Parameters.AddWithValue("e", idEmpresa);
                nroNc = Convert.ToInt64(await q.ExecuteScalarAsync(ct));
            }
            else nroNc = Convert.ToInt64(s);
        }

        decimal montoTotal = 0, montoIva = 0;
        foreach (var l in lines)
        {
            var cant = Math.Max(1, l.CantEmb);
            montoTotal += (decimal)(l.PrecioNetoCon * cant);
        }

        long idNc;
        await using (var cmd = new NpgsqlCommand(
                         """
                         INSERT INTO dbo.notacredito (
                             idempresa, idcuenta, idcotizacioncomercial, idmotivondnc,
                             tiponotacredito, fechanotacredito, fechaemision, montocambio,
                             montototal, montoiva, montoexento, montogravado, montocosto, montocomision,
                             estado, aplicarfactura, notacreditoporfactura, porcentajedescuento,
                             iddeposito, idpedido, nronotacredito, activo, ultmodificacion, username, observacion
                         ) VALUES (
                             @e, @cta, @cot, @mot,
                             0, NOW(), NOW(), 1,
                             @tot, @iva, 0, @tot, 0, 0,
                             1, true, true, 0,
                             @dep, @ped, @nro, true, NOW(), 'ecom-devolucion', @obs
                         )
                         RETURNING id
                         """, conn, tx))
        {
            cmd.Parameters.AddWithValue("e", idEmpresa);
            cmd.Parameters.AddWithValue("cta", factura.IdCuenta);
            cmd.Parameters.AddWithValue("cot", factura.IdCotizacion);
            cmd.Parameters.AddWithValue("mot", idMotivo);
            cmd.Parameters.AddWithValue("tot", (double)montoTotal);
            cmd.Parameters.AddWithValue("iva", (double)montoIva);
            cmd.Parameters.AddWithValue("dep", factura.IdDeposito > 0 ? factura.IdDeposito : local.IDDeposito);
            cmd.Parameters.AddWithValue("ped", factura.IdPedido);
            cmd.Parameters.AddWithValue("nro", nroNc);
            cmd.Parameters.AddWithValue("obs", Truncate(motivo ?? $"Devolución e-com pedido {local.NroPedido}", 480));
            idNc = Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
        }

        foreach (var l in lines)
        {
            var cant = Math.Max(1d, l.CantEmb + (l.CantFrac > 0 ? l.CantFrac / Math.Max(1d, l.CantXEmb) : 0));
            await using var cmd = new NpgsqlCommand(
                """
                INSERT INTO dbo.detallenotacredito (
                    idempresa, idnotacredito, idfactura, idpedido, idarticuloembalaje,
                    cantidadxembalaje, idregimenimpositivo, porcentajeimpuesto,
                    cantidad, cantidadembalada, cantidadfraccionada,
                    preciobrutoconimpuesto, preciobrutosinimpuesto,
                    precionetoconimpuesto, precionetosinimpuesto,
                    montototal, montoimpuesto, montoexento, montogravado, montocosto, montocomision,
                    costogestionultimo, costogestionpromedio, costogestionfifo, costogestionlifo, costogestionnifo,
                    costoultimo, costopromedio, costofifo, costolifo, costonifo, indicecomision,
                    activo, ultmodificacion, username
                ) VALUES (
                    @e, @nc, @fac, @ped, @ae,
                    @cxe, @reg, @pimp,
                    @cant, @ce, @cf,
                    @pbc, @pbs, @pnc, @pns,
                    @mt, @mi, @me, @mg, 0, 0,
                    0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0,
                    true, NOW(), 'ecom-devolucion'
                )
                """, conn, tx);
            cmd.Parameters.AddWithValue("e", idEmpresa);
            cmd.Parameters.AddWithValue("nc", idNc);
            cmd.Parameters.AddWithValue("fac", factura.IdFactura);
            cmd.Parameters.AddWithValue("ped", factura.IdPedido);
            cmd.Parameters.AddWithValue("ae", l.IdArticuloEmbalaje);
            cmd.Parameters.AddWithValue("cxe", l.CantXEmb);
            cmd.Parameters.AddWithValue("reg", l.IdRegimen);
            cmd.Parameters.AddWithValue("pimp", l.PorcImp);
            cmd.Parameters.AddWithValue("cant", cant);
            cmd.Parameters.AddWithValue("ce", l.CantEmb);
            cmd.Parameters.AddWithValue("cf", l.CantFrac);
            cmd.Parameters.AddWithValue("pbc", l.PrecioBrutoCon);
            cmd.Parameters.AddWithValue("pbs", l.PrecioBrutoSin);
            cmd.Parameters.AddWithValue("pnc", l.PrecioNetoCon);
            cmd.Parameters.AddWithValue("pns", l.PrecioNetoSin);
            cmd.Parameters.AddWithValue("mt", (double)l.MontoTotal);
            cmd.Parameters.AddWithValue("mi", (double)l.MontoImpuesto);
            cmd.Parameters.AddWithValue("me", (double)l.MontoExento);
            cmd.Parameters.AddWithValue("mg", (double)l.MontoGravado);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        return idNc;
    }

    private static async Task RestoreErpStockFromDetalleAsync(
        NpgsqlConnection conn, NpgsqlTransaction tx,
        long idEmpresa, long erpPmId, long idDepositoFallback, bool asDevolucion, CancellationToken ct)
    {
        var lines = new List<(long Art, long Dep, int Cant)>();
        await using (var cmd = new NpgsqlCommand(
                         """
                         SELECT d.idarticulo, COALESCE(pm.iddeposito, @dep),
                                GREATEST(COALESCE(d.cantidadembalada, 0), 1)
                         FROM dbo.detallepedidomovil d
                         INNER JOIN dbo.pedidomovil pm ON pm.id = d.idpedidomovil
                         WHERE d.idpedidomovil = @pm AND COALESCE(d.activo, true) = true
                         """, conn, tx))
        {
            cmd.Parameters.AddWithValue("pm", erpPmId);
            cmd.Parameters.AddWithValue("dep", idDepositoFallback);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                lines.Add((r.GetInt64(0), r.GetInt64(1), Convert.ToInt32(r.GetValue(2))));
        }

        foreach (var (art, dep, cant) in lines)
        {
            if (asDevolucion)
            {
                await using var cmd = new NpgsqlCommand(
                    """
                    UPDATE dbo.stock
                    SET totalexistencia = COALESCE(totalexistencia, 0) + @c,
                        totaldevolucionventa = COALESCE(totaldevolucionventa, 0) + @c,
                        ultmodificacion = NOW(),
                        username = 'ecom-devolucion'
                    WHERE idempresa = @e AND idarticulo = @a AND iddeposito = @d
                    """, conn, tx);
                cmd.Parameters.AddWithValue("c", (double)cant);
                cmd.Parameters.AddWithValue("e", idEmpresa);
                cmd.Parameters.AddWithValue("a", art);
                cmd.Parameters.AddWithValue("d", dep);
                await cmd.ExecuteNonQueryAsync(ct);
            }
            else
            {
                await using var cmd = new NpgsqlCommand(
                    """
                    UPDATE dbo.stock
                    SET totalexistencia = COALESCE(totalexistencia, 0) + @c,
                        totalpedidoventa = GREATEST(0, COALESCE(totalpedidoventa, 0) - @c),
                        ultmodificacion = NOW(),
                        username = 'ecom-cancel'
                    WHERE idempresa = @e AND idarticulo = @a AND iddeposito = @d
                    """, conn, tx);
                cmd.Parameters.AddWithValue("c", (double)cant);
                cmd.Parameters.AddWithValue("e", idEmpresa);
                cmd.Parameters.AddWithValue("a", art);
                cmd.Parameters.AddWithValue("d", dep);
                await cmd.ExecuteNonQueryAsync(ct);
            }
        }
    }

    private static string Truncate(string? v, int max)
    {
        if (string.IsNullOrEmpty(v)) return "";
        return v.Length <= max ? v : v[..max];
    }
}
