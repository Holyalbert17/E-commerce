using Datamex.ECommerce.Application.Sync;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Datamex.ECommerce.Infrastructure.Sync;

/// <summary>
/// Write-back de cuenta al ERP alineado a UsuarioEcommerceService.ActivarUsuario:
/// link por email/id, o INSERT cliente+cuenta clonando FKs de una cuenta plantilla de la empresa.
/// </summary>
public sealed class CuentaErpWriter : ICuentaErpWriter
{
    private readonly EcomDbContext _db;
    private readonly IErpConnectionResolver _erpCs;
    private readonly ILogger<CuentaErpWriter> _log;

    public CuentaErpWriter(EcomDbContext db, IErpConnectionResolver erpCs, ILogger<CuentaErpWriter> log)
    {
        _db = db;
        _erpCs = erpCs;
        _log = log;
    }

    public async Task<long?> EnsureCuentaInErpAsync(Cuenta cuenta, UsuarioEcommerce user, CancellationToken ct = default)
    {
        var config = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == cuenta.IDEmpresa, ct);
        if (config is null || !config.ProcesaPedidosERP)
        {
            _log.LogInformation(
                "Cuenta #{Nro} local-only (ProcesaPedidosERP=false) empresa={Empresa}",
                cuenta.NroCuenta, cuenta.IDEmpresa);
            return null;
        }

        var erpCs = await _erpCs.ResolveAsync(cuenta.IDEmpresa, ct);
        var allow = await _erpCs.AllowWriteBackAsync(cuenta.IDEmpresa, ct);
        if (string.IsNullOrWhiteSpace(erpCs) || !allow)
        {
            _log.LogInformation(
                "Cuenta #{Nro} no escrita en ERP: AllowWriteBack=false o sin CS ERP (empresa={Empresa}).",
                cuenta.NroCuenta, cuenta.IDEmpresa);
            return null;
        }

        try
        {
            return await WriteCuentaCoreAsync(erpCs, cuenta, user, ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex,
                "No se pudo insertar cuenta en ERP (local id={Id}, nro={Nro}). Activación local OK.",
                cuenta.ID, cuenta.NroCuenta);
            return null;
        }
    }

    public async Task<long?> EnsureCuentaInErpByIdAsync(long idEmpresa, long idCuenta, CancellationToken ct = default)
    {
        var cuenta = await _db.Cuentas.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idCuenta && c.IDEmpresa == idEmpresa && c.Activo, ct);
        if (cuenta is null)
        {
            _log.LogWarning("EnsureCuentaInErpById: cuenta {Id} no existe localmente (empresa {E}).", idCuenta, idEmpresa);
            return null;
        }

        var link = await _db.UsuariosEcommerceCuenta.AsNoTracking()
            .Where(l => l.IDEmpresa == idEmpresa && l.IDCuenta == idCuenta && l.Activo)
            .OrderByDescending(l => l.EsPrincipal)
            .FirstOrDefaultAsync(ct);

        UsuarioEcommerce? user = null;
        if (link is not null)
        {
            user = await _db.UsuariosEcommerce.AsNoTracking()
                .FirstOrDefaultAsync(u => u.ID == link.IDUsuarioEcommerce && u.IDEmpresa == idEmpresa, ct);
        }

        user ??= new UsuarioEcommerce
        {
            ID = 0,
            IDEmpresa = idEmpresa,
            Nombre = cuenta.NombreCuenta ?? "Cliente",
            Apellido = cuenta.ApellidoCuenta ?? "ECOM",
            Email = cuenta.Email ?? "",
            Usuario = cuenta.Email ?? $"cta-{cuenta.ID}",
            NumeroDocumento = null,
            IDTipoDocumento = null
        };

        return await EnsureCuentaInErpAsync(cuenta, user, ct);
    }

    private async Task<long?> WriteCuentaCoreAsync(
        string erpCs, Cuenta cuenta, UsuarioEcommerce user, CancellationToken ct)
    {
        try
        {
            await using var conn = new NpgsqlConnection(erpCs);
            await conn.OpenAsync(ct);
            await using (var setPath = new NpgsqlCommand("SET search_path TO dbo, public", conn))
                await setPath.ExecuteNonQueryAsync(ct);

            // 1) Link si ya existe (id / email / documento)
            await using (var find = new NpgsqlCommand(
                             """
                             SELECT id FROM dbo.cuenta
                             WHERE id = @id
                                OR (COALESCE(email,'') <> '' AND lower(email) = lower(@email))
                             LIMIT 1
                             """, conn))
            {
                find.Parameters.AddWithValue("id", cuenta.ID);
                find.Parameters.AddWithValue("email", (object?)(cuenta.Email ?? user.Email) ?? "");
                var existing = await find.ExecuteScalarAsync(ct);
                if (existing is not null && existing is not DBNull)
                {
                    var id = Convert.ToInt64(existing);
                    _log.LogInformation("Cuenta ERP ya existía id={Id} (local={Local})", id, cuenta.ID);
                    return id;
                }
            }

            if (user.IDTipoDocumento is > 0 && !string.IsNullOrWhiteSpace(user.NumeroDocumento))
            {
                await using var findDoc = new NpgsqlCommand(
                    """
                    SELECT c.id
                    FROM dbo.cuenta c
                    INNER JOIN dbo.cuentadocumento d ON d.idcuenta = c.id AND d.activo = true
                    WHERE c.idempresa = @e
                      AND d.idtipodocumento = @td
                      AND trim(d.numerodocumento) = trim(@nro)
                    ORDER BY c.nrocuenta
                    LIMIT 1
                    """, conn);
                findDoc.Parameters.AddWithValue("e", cuenta.IDEmpresa);
                findDoc.Parameters.AddWithValue("td", user.IDTipoDocumento.Value);
                findDoc.Parameters.AddWithValue("nro", user.NumeroDocumento.Trim());
                var byDoc = await findDoc.ExecuteScalarAsync(ct);
                if (byDoc is not null && byDoc is not DBNull)
                {
                    var id = Convert.ToInt64(byDoc);
                    _log.LogInformation(
                        "Cuenta ERP encontrada por documento id={Id} (local={Local})", id, cuenta.ID);
                    return id;
                }
            }

            // 2) Plantilla de FKs obligatorias (paridad ActivarUsuario)
            await using var tplCmd = new NpgsqlCommand(
                """
                SELECT idcliente, contador, idvendedor, idzona, idbarrio, idciudad, iddistrito,
                       iddivisionpolitica, iddivision, idramo, idcanal, idcondicionventa,
                       idtipopersona, idsituacioncliente, estado, idlistaprecio, idagencia,
                       idsubsegmentomkt, tipofactura
                FROM dbo.cuenta
                WHERE idempresa = @e AND activo = true
                ORDER BY id
                LIMIT 1
                """, conn);
            tplCmd.Parameters.AddWithValue("e", cuenta.IDEmpresa);

            await using var tpl = await tplCmd.ExecuteReaderAsync(ct);
            if (!await tpl.ReadAsync(ct))
            {
                _log.LogWarning(
                    "No hay cuenta plantilla en ERP empresa={Empresa}; no se puede insertar con FKs.",
                    cuenta.IDEmpresa);
                return null;
            }

            var idVendedor = tpl.GetInt64(tpl.GetOrdinal("idvendedor"));
            var idZona = tpl.GetInt64(tpl.GetOrdinal("idzona"));
            var idBarrio = cuenta.IDBarrio ?? tpl.GetInt64(tpl.GetOrdinal("idbarrio"));
            var idCiudad = cuenta.IDCiudad ?? tpl.GetInt64(tpl.GetOrdinal("idciudad"));
            var idDistrito = tpl.GetInt64(tpl.GetOrdinal("iddistrito"));
            var idDivPol = cuenta.IDDivisionPolitica ?? tpl.GetInt64(tpl.GetOrdinal("iddivisionpolitica"));
            var idDivision = tpl.GetInt64(tpl.GetOrdinal("iddivision"));
            var idRamo = tpl.GetInt64(tpl.GetOrdinal("idramo"));
            var idCanal = tpl.GetInt64(tpl.GetOrdinal("idcanal"));
            var idCond = tpl.GetInt64(tpl.GetOrdinal("idcondicionventa"));
            var idTipoPersona = tpl.GetInt64(tpl.GetOrdinal("idtipopersona"));
            var idSit = tpl.GetInt64(tpl.GetOrdinal("idsituacioncliente"));
            // Paridad ActivarUsuario ERP: Estado = ACTIVO (1), no heredar INACTIVO de plantilla.
            const int estadoActivo = 1;
            var idLista = cuenta.IDListaPrecio
                          ?? (tpl.IsDBNull(tpl.GetOrdinal("idlistaprecio"))
                              ? (long?)null
                              : tpl.GetInt64(tpl.GetOrdinal("idlistaprecio")));
            var idAgencia = cuenta.IDAgencia
                            ?? (tpl.IsDBNull(tpl.GetOrdinal("idagencia"))
                                ? (long?)null
                                : tpl.GetInt64(tpl.GetOrdinal("idagencia")));
            var idSub = tpl.IsDBNull(tpl.GetOrdinal("idsubsegmentomkt"))
                ? (long?)null
                : tpl.GetInt64(tpl.GetOrdinal("idsubsegmentomkt"));
            // Paridad ActivarUsuario: TipoFactura = REPARTO (0).
            const int tipoFacturaReparto = 0;
            await tpl.DisposeAsync();

            await using var tx = await conn.BeginTransactionAsync(ct);

            // 3) Cliente (paridad ActivarUsuario)
            long codCliente;
            await using (var next = new NpgsqlCommand(
                             """
                             SELECT COALESCE(MAX(codcliente), 0) + 1
                             FROM dbo.cliente WHERE idempresa = @e
                             """, conn, tx))
            {
                next.Parameters.AddWithValue("e", cuenta.IDEmpresa);
                codCliente = Convert.ToInt64(await next.ExecuteScalarAsync(ct));
            }

            var nombre = (cuenta.NombreCuenta ?? user.Nombre ?? "Cliente").Trim().ToUpperInvariant();
            var apellido = (cuenta.ApellidoCuenta ?? user.Apellido ?? "ECOM").Trim().ToUpperInvariant();
            var razon = string.IsNullOrWhiteSpace(cuenta.RazonSocial)
                ? $"{apellido} {nombre}"
                : cuenta.RazonSocial.Trim().ToUpperInvariant();
            var email = (cuenta.Email ?? user.Email ?? "").Trim().ToLowerInvariant();
            var nroCuenta = cuenta.NroCuenta > 0 ? cuenta.NroCuenta : codCliente * 100 + 1;

            long idCliente;
            await using (var insCli = new NpgsqlCommand(
                             """
                             INSERT INTO dbo.cliente (
                               activo, ultmodificacion, username, idempresa,
                               codcliente, razonsocial, apellidocliente, nombrecliente,
                               idsituacioncliente, idtipocliente, estado
                             ) VALUES (
                               true, @ult, 'ecom-activar', @e,
                               @cod, @razon, @ape, @nom,
                               @sit, @tipocli, @estado
                             ) RETURNING id
                             """, conn, tx))
            {
                // idtipocliente: tomar de un cliente plantilla
                long idTipoCliente;
                await using (var tcli = new NpgsqlCommand(
                                 """
                                 SELECT idtipocliente FROM dbo.cliente
                                 WHERE idempresa = @e AND activo = true
                                 ORDER BY id LIMIT 1
                                 """, conn, tx))
                {
                    tcli.Parameters.AddWithValue("e", cuenta.IDEmpresa);
                    var tc = await tcli.ExecuteScalarAsync(ct);
                    if (tc is null || tc is DBNull)
                        throw new InvalidOperationException("ERP sin tipocliente plantilla.");
                    idTipoCliente = Convert.ToInt64(tc);
                }

                insCli.Parameters.AddWithValue("ult", DateTime.UtcNow);
                insCli.Parameters.AddWithValue("e", cuenta.IDEmpresa);
                insCli.Parameters.AddWithValue("cod", (int)codCliente);
                insCli.Parameters.AddWithValue("razon", Trunc(razon, 255));
                insCli.Parameters.AddWithValue("ape", Trunc(apellido, 100));
                insCli.Parameters.AddWithValue("nom", Trunc(nombre, 100));
                insCli.Parameters.AddWithValue("sit", idSit);
                insCli.Parameters.AddWithValue("tipocli", idTipoCliente);
                insCli.Parameters.AddWithValue("estado", estadoActivo);
                idCliente = Convert.ToInt64(await insCli.ExecuteScalarAsync(ct));
            }

            // 4) Cuenta con FKs obligatorias (ID local para PedidoMovil.IDCuenta)
            await using (var insCta = new NpgsqlCommand(
                             """
                             INSERT INTO dbo.cuenta (
                               id, activo, ultmodificacion, username, idempresa,
                               idcliente, nrocuenta, contador, razonsocial, nombrecuenta, apellidocuenta,
                               direccion, email, contacto,
                               idvendedor, idzona, nroordenenzona,
                               idbarrio, idciudad, iddistrito, iddivisionpolitica,
                               iddivision, idramo, idcanal, idsubsegmentomkt,
                               idcondicionventa, idtipopersona, idsituacioncliente,
                               idlistaprecio, idagencia, tipofactura, estado,
                               fechaingreso, latitud, longitud, precision,
                               porcdescuentoautorizado, plazocobroapremiante, plazovencimiento,
                               nroclientesistemaexterno
                             ) VALUES (
                               @id, true, @ult, 'ecom-activar', @e,
                               @idcliente, @nro, 1, @razon, @nom, @ape,
                               @dir, @email, @contacto,
                               @vend, @zona, 1,
                               @barrio, @ciudad, @distrito, @divpol,
                               @division, @ramo, @canal, @sub,
                               @cond, @tipoper, @sit,
                               @lista, @agencia, @tipofact, @estado,
                               @fecha, @lat, @lng, 17,
                               0, 0, 0,
                               @nroext
                             )
                             ON CONFLICT (id) DO NOTHING
                             RETURNING id
                             """, conn, tx))
            {
                insCta.Parameters.AddWithValue("id", cuenta.ID);
                insCta.Parameters.AddWithValue("ult", DateTime.UtcNow);
                insCta.Parameters.AddWithValue("e", cuenta.IDEmpresa);
                insCta.Parameters.AddWithValue("idcliente", idCliente);
                insCta.Parameters.AddWithValue("nro", nroCuenta);
                insCta.Parameters.AddWithValue("razon", Trunc(razon, 255));
                insCta.Parameters.AddWithValue("nom", Trunc(nombre, 100));
                insCta.Parameters.AddWithValue("ape", Trunc(apellido, 100));
                insCta.Parameters.AddWithValue("dir", (object?)(cuenta.Direccion ?? "") ?? "");
                insCta.Parameters.AddWithValue("email", string.IsNullOrWhiteSpace(email) ? DBNull.Value : email);
                insCta.Parameters.AddWithValue("contacto", Trunc(razon, 100));
                insCta.Parameters.AddWithValue("vend", idVendedor);
                insCta.Parameters.AddWithValue("zona", idZona);
                insCta.Parameters.AddWithValue("barrio", idBarrio);
                insCta.Parameters.AddWithValue("ciudad", idCiudad);
                insCta.Parameters.AddWithValue("distrito", idDistrito);
                insCta.Parameters.AddWithValue("divpol", idDivPol);
                insCta.Parameters.AddWithValue("division", idDivision);
                insCta.Parameters.AddWithValue("ramo", idRamo);
                insCta.Parameters.AddWithValue("canal", idCanal);
                insCta.Parameters.AddWithValue("sub", (object?)idSub ?? DBNull.Value);
                insCta.Parameters.AddWithValue("cond", idCond);
                insCta.Parameters.AddWithValue("tipoper", idTipoPersona);
                insCta.Parameters.AddWithValue("sit", idSit);
                insCta.Parameters.AddWithValue("lista", (object?)idLista ?? DBNull.Value);
                insCta.Parameters.AddWithValue("agencia", (object?)idAgencia ?? DBNull.Value);
                insCta.Parameters.AddWithValue("tipofact", tipoFacturaReparto);
                insCta.Parameters.AddWithValue("estado", estadoActivo);
                insCta.Parameters.AddWithValue("fecha", DateTime.UtcNow);
                insCta.Parameters.AddWithValue("lat", (object?)cuenta.Latitud ?? DBNull.Value);
                insCta.Parameters.AddWithValue("lng", (object?)cuenta.Longitud ?? DBNull.Value);
                insCta.Parameters.AddWithValue("nroext", nroCuenta.ToString());

                var inserted = await insCta.ExecuteScalarAsync(ct);
                var idCuenta = inserted is not null && inserted is not DBNull
                    ? Convert.ToInt64(inserted)
                    : cuenta.ID;

                // 5) Documento + teléfonos (paridad ActivarUsuario)
                await InsertDocumentoYTelefonosAsync(
                    conn, tx, cuenta.IDEmpresa, idCuenta, user, ct);

                // 6) listapreciocuenta (Import/Pedido ERP la usan para cotización de la lista)
                if (idLista is > 0)
                {
                    await using var insLpc = new NpgsqlCommand(
                        """
                        INSERT INTO dbo.listapreciocuenta (
                          idempresa, idlistaprecio, idcuenta, activo, ultmodificacion, username
                        )
                        SELECT @e, @lista, @cuenta, true, @ult, 'ecom-activar'
                        WHERE NOT EXISTS (
                          SELECT 1 FROM dbo.listapreciocuenta
                          WHERE idcuenta = @cuenta AND idlistaprecio = @lista AND activo
                        )
                        """, conn, tx);
                    insLpc.Parameters.AddWithValue("e", cuenta.IDEmpresa);
                    insLpc.Parameters.AddWithValue("lista", idLista.Value);
                    insLpc.Parameters.AddWithValue("cuenta", idCuenta);
                    insLpc.Parameters.AddWithValue("ult", DateTime.UtcNow);
                    await insLpc.ExecuteNonQueryAsync(ct);
                }

                await tx.CommitAsync(ct);

                _log.LogInformation(
                    "Cuenta+Cliente ERP insertados (paridad ActivarUsuario) id={Id} cliente={Cli} nro={Nro}",
                    idCuenta, idCliente, nroCuenta);
                return idCuenta;
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex,
                "No se pudo insertar cuenta en ERP (local id={Id}, nro={Nro}). Activación local OK.",
                cuenta.ID, cuenta.NroCuenta);
            return null;
        }
    }

    private async Task InsertDocumentoYTelefonosAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        long idEmpresa,
        long idCuenta,
        UsuarioEcommerce user,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        if (user.IDTipoDocumento is > 0 && !string.IsNullOrWhiteSpace(user.NumeroDocumento))
        {
            await using var insDoc = new NpgsqlCommand(
                """
                INSERT INTO dbo.cuentadocumento (
                  activo, ultmodificacion, username, idempresa,
                  idcuenta, idtipodocumento, numerodocumento,
                  fechavencimiento, paraemisioncomprobantes
                ) VALUES (
                  true, @ult, 'ecom-activar', @e,
                  @cta, @td, @nro,
                  @venc, true
                )
                """, conn, tx);
            insDoc.Parameters.AddWithValue("ult", now);
            insDoc.Parameters.AddWithValue("e", idEmpresa);
            insDoc.Parameters.AddWithValue("cta", idCuenta);
            insDoc.Parameters.AddWithValue("td", user.IDTipoDocumento.Value);
            insDoc.Parameters.AddWithValue("nro", Trunc(user.NumeroDocumento.Trim(), 25));
            insDoc.Parameters.AddWithValue("venc", now.Date.AddYears(100));
            await insDoc.ExecuteNonQueryAsync(ct);
        }
        else
        {
            _log.LogDebug("Sin documento para cuenta ERP {Cta}; se omite cuentadocumento.", idCuenta);
        }

        long? tipo1 = null;
        long? tipo2 = null;
        await using (var tipos = new NpgsqlCommand(
                         """
                         SELECT id FROM dbo.tipotelefono
                         WHERE idempresa = @e AND activo = true
                         ORDER BY codtipotelefono NULLS LAST, id
                         LIMIT 2
                         """, conn, tx))
        {
            tipos.Parameters.AddWithValue("e", idEmpresa);
            await using var r = await tipos.ExecuteReaderAsync(ct);
            if (await r.ReadAsync(ct))
                tipo1 = r.GetInt64(0);
            if (await r.ReadAsync(ct))
                tipo2 = r.GetInt64(0);
        }

        if (tipo1 is null)
        {
            _log.LogDebug("Sin tipotelefono en ERP empresa={E}; se omiten teléfonos.", idEmpresa);
            return;
        }

        var tel = string.IsNullOrWhiteSpace(user.NumeroTelefono) ? null : user.NumeroTelefono.Trim();
        var movil = string.IsNullOrWhiteSpace(user.NumeroMovil) ? null : user.NumeroMovil.Trim();

        if (!string.IsNullOrWhiteSpace(tel))
            await InsertTelefonoAsync(conn, tx, idEmpresa, idCuenta, tipo1.Value, tel, now, ct);

        if (!string.IsNullOrWhiteSpace(movil))
            await InsertTelefonoAsync(conn, tx, idEmpresa, idCuenta, tipo2 ?? tipo1.Value, movil, now, ct);
        else if (!string.IsNullOrWhiteSpace(tel) && tipo2 is not null)
            await InsertTelefonoAsync(conn, tx, idEmpresa, idCuenta, tipo2.Value, tel, now, ct);
    }

    private static async Task InsertTelefonoAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        long idEmpresa,
        long idCuenta,
        long idTipoTelefono,
        string numero,
        DateTime now,
        CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO dbo.cuentatelefono (
              activo, ultmodificacion, username, idempresa,
              idcuenta, idtipotelefono, numerotelefono
            ) VALUES (
              true, @ult, 'ecom-activar', @e,
              @cta, @tipo, @nro
            )
            """, conn, tx);
        cmd.Parameters.AddWithValue("ult", now);
        cmd.Parameters.AddWithValue("e", idEmpresa);
        cmd.Parameters.AddWithValue("cta", idCuenta);
        cmd.Parameters.AddWithValue("tipo", idTipoTelefono);
        cmd.Parameters.AddWithValue("nro", Trunc(numero, 25));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static string Trunc(string s, int max) =>
        s.Length <= max ? s : s[..max];
}
