using Datamex.ECommerce.Application.Sync;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Text;

namespace Datamex.ECommerce.Infrastructure.Sync;

public sealed class RegistroClienteErpLookup : IRegistroClienteErpLookup
{
    private readonly EcomDbContext _db;
    private readonly IErpConnectionResolver _erpCs;
    private readonly ILogger<RegistroClienteErpLookup> _log;

    public RegistroClienteErpLookup(
        EcomDbContext db,
        IErpConnectionResolver erpCs,
        ILogger<RegistroClienteErpLookup> log)
    {
        _db = db;
        _erpCs = erpCs;
        _log = log;
    }

    public static string NormalizeDocumento(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value.Trim().ToUpperInvariant())
        {
            if (char.IsLetterOrDigit(ch))
                sb.Append(ch);
        }
        return sb.ToString();
    }

    public async Task<ErpClienteMatch> FindAsync(
        long idEmpresa,
        string email,
        string numeroDocumentoNormalizado,
        CancellationToken ct = default)
    {
        var cs = await _erpCs.ResolveAsync(idEmpresa, ct);
        if (string.IsNullOrWhiteSpace(cs))
            return ErpClienteMatch.None;

        try
        {
            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync(ct);
            await using (var setPath = new NpgsqlCommand("SET search_path TO dbo, public", conn))
                await setPath.ExecuteNonQueryAsync(ct);

            var byEmail = await FindIdsByEmailAsync(conn, idEmpresa, email, ct);
            var byDoc = string.IsNullOrEmpty(numeroDocumentoNormalizado)
                ? Array.Empty<long>()
                : await FindIdsByDocumentoAsync(conn, idEmpresa, numeroDocumentoNormalizado, ct);

            if (byEmail.Length > 1)
                return new ErpClienteMatch(null, null,
                    "Hay más de un cliente en el ERP con ese correo. Contactá a la empresa.");
            if (byDoc.Length > 1)
                return new ErpClienteMatch(null, null,
                    "Hay más de un cliente en el ERP con ese documento/RUC. Contactá a la empresa.");

            long? emailId = byEmail.Length == 1 ? byEmail[0] : null;
            long? docId = byDoc.Length == 1 ? byDoc[0] : null;

            if (emailId is > 0 && docId is > 0 && emailId != docId)
                return new ErpClienteMatch(null, null,
                    "El correo y el RUC corresponden a clientes distintos en el ERP. Contactá a la empresa.");

            if (docId is > 0)
                return new ErpClienteMatch(docId, emailId == docId ? "ambos" : "ruc");
            if (emailId is > 0)
                return new ErpClienteMatch(emailId, "email");
            return ErpClienteMatch.None;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Lookup cliente ERP falló empresa={Empresa}; se continúa solo con e-com.", idEmpresa);
            return ErpClienteMatch.None;
        }
    }

    public async Task<Cuenta?> ImportCuentaAsync(long idEmpresa, long idCuentaErp, CancellationToken ct = default)
    {
        var cs = await _erpCs.ResolveAsync(idEmpresa, ct);
        if (string.IsNullOrWhiteSpace(cs))
            return null;

        await using var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync(ct);
        await using (var setPath = new NpgsqlCommand("SET search_path TO dbo, public", conn))
            await setPath.ExecuteNonQueryAsync(ct);

        await using var cmd = new NpgsqlCommand(
            """
            SELECT c.id, c.nrocuenta, c.razonsocial, c.nombrecuenta, c.apellidocuenta,
                   c.direccion, c.email, c.idciudad, c.idbarrio, c.iddivisionpolitica,
                   c.idlistaprecio, c.idagencia, c.latitud, c.longitud,
                   c.activo, c.ultmodificacion, c.username
            FROM dbo.cuenta c
            WHERE c.id = @id AND c.idempresa = @e
            LIMIT 1
            """, conn);
        cmd.Parameters.AddWithValue("id", idCuentaErp);
        cmd.Parameters.AddWithValue("e", idEmpresa);

        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct))
            return null;

        var now = DateTime.UtcNow;
        var id = r.GetInt64(0);
        var nro = r.IsDBNull(1) ? id : Convert.ToInt64(r.GetValue(1));
        var razon = Trunc(ReadString(r, 2), 255) ?? "";
        var nombre = ReadString(r, 3);
        var apellido = ReadString(r, 4);
        var direccion = ReadString(r, 5);
        var email = ReadString(r, 6);
        var idCiudad = ReadLongNull(r, 7);
        var idBarrio = ReadLongNull(r, 8);
        var idDiv = ReadLongNull(r, 9);
        var idLista = ReadLongNull(r, 10);
        var idAgencia = ReadLongNull(r, 11);
        var lat = ReadDecimalNull(r, 12);
        var lng = ReadDecimalNull(r, 13);
        var activo = r.IsDBNull(14) || r.GetBoolean(14);
        await r.CloseAsync();

        if (idLista is > 0 && !await _db.ListasPrecio.AnyAsync(l => l.ID == idLista, ct))
            idLista = null;
        if (idAgencia is > 0 && !await _db.Agencias.AnyAsync(a => a.ID == idAgencia, ct))
            idAgencia = null;
        if (idCiudad is > 0 && !await _db.Ciudades.AnyAsync(c => c.ID == idCiudad, ct))
            idCiudad = null;
        if (idBarrio is > 0 && !await _db.Barrios.AnyAsync(b => b.ID == idBarrio, ct))
            idBarrio = null;
        if (idDiv is > 0 && !await _db.DivisionesPoliticas.AnyAsync(d => d.ID == idDiv, ct))
            idDiv = null;

        var cuenta = await _db.Cuentas.FirstOrDefaultAsync(c => c.ID == id, ct);
        if (cuenta is null)
        {
            cuenta = new Cuenta { ID = id, IDEmpresa = idEmpresa };
            _db.Cuentas.Add(cuenta);
        }

        cuenta.IDEmpresa = idEmpresa;
        cuenta.NroCuenta = nro;
        cuenta.RazonSocial = razon;
        cuenta.NombreCuenta = nombre;
        cuenta.ApellidoCuenta = apellido;
        cuenta.Direccion = direccion;
        cuenta.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        cuenta.IDCiudad = idCiudad;
        cuenta.IDBarrio = idBarrio;
        cuenta.IDDivisionPolitica = idDiv;
        cuenta.IDListaPrecio = idLista;
        cuenta.IDAgencia = idAgencia;
        cuenta.Latitud = lat;
        cuenta.Longitud = lng;
        cuenta.Activo = activo;
        cuenta.UltModificacion = now;
        cuenta.UserName = "erp-sync";
        await _db.SaveChangesAsync(ct);

        await _db.Database.ExecuteSqlRawAsync(
            """
            SELECT setval(
              pg_get_serial_sequence('dbo.cuenta', 'ID'),
              GREATEST((SELECT COALESCE(MAX("ID"), 1) FROM dbo.cuenta), 1),
              true)
            """, ct);

        if (idLista is > 0)
            await EnsureListaPrecioCuentaAsync(conn, idEmpresa, id, idLista.Value, now, ct);

        _log.LogInformation(
            "Cuenta ERP {Id} migrada a e-com empresa={Empresa} nro={Nro} (facturación).",
            id, idEmpresa, nro);
        return cuenta;
    }

    private async Task EnsureListaPrecioCuentaAsync(
        NpgsqlConnection conn,
        long idEmpresa,
        long idCuenta,
        long idLista,
        DateTime now,
        CancellationToken ct)
    {
        var local = await _db.ListasPrecioCuenta.FirstOrDefaultAsync(
            x => x.IDCuenta == idCuenta && x.IDListaPrecio == idLista && x.IDEmpresa == idEmpresa, ct);
        if (local is not null)
            return;

        long? erpLpcId = null;
        await using (var find = new NpgsqlCommand(
                         """
                         SELECT id FROM dbo.listapreciocuenta
                         WHERE idcuenta = @c AND idlistaprecio = @l AND activo
                         ORDER BY id DESC LIMIT 1
                         """, conn))
        {
            find.Parameters.AddWithValue("c", idCuenta);
            find.Parameters.AddWithValue("l", idLista);
            var v = await find.ExecuteScalarAsync(ct);
            if (v is not null and not DBNull)
                erpLpcId = Convert.ToInt64(v);
        }

        var row = new ListaPrecioCuenta
        {
            IDEmpresa = idEmpresa,
            IDCuenta = idCuenta,
            IDListaPrecio = idLista,
            Activo = true,
            UltModificacion = now,
            UserName = "erp-sync"
        };
        if (erpLpcId is > 0)
        {
            var clash = await _db.ListasPrecioCuenta.FindAsync([erpLpcId.Value], ct);
            if (clash is null)
                row.ID = erpLpcId.Value;
        }
        _db.ListasPrecioCuenta.Add(row);
        await _db.SaveChangesAsync(ct);
    }

    private static async Task<long[]> FindIdsByEmailAsync(
        NpgsqlConnection conn, long idEmpresa, string email, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            """
            SELECT DISTINCT c.id
            FROM dbo.cuenta c
            WHERE c.idempresa = @e AND COALESCE(c.activo, true)
              AND (
                (COALESCE(c.email, '') <> '' AND lower(c.email) = lower(@email))
                OR EXISTS (
                  SELECT 1
                  FROM dbo.usuarioecommerce u
                  JOIN dbo.usuarioecommercecuenta l ON l.idusuarioecommerce = u.id
                  WHERE l.idcuenta = c.id
                    AND u.idempresa = @e
                    AND lower(COALESCE(u.email, u.usuario, '')) = lower(@email)
                )
              )
            ORDER BY c.id
            LIMIT 5
            """, conn);
        cmd.Parameters.AddWithValue("e", idEmpresa);
        cmd.Parameters.AddWithValue("email", email);
        return await ReadIdsAsync(cmd, ct);
    }

    private static async Task<long[]> FindIdsByDocumentoAsync(
        NpgsqlConnection conn, long idEmpresa, string docNorm, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            """
            SELECT DISTINCT c.id
            FROM dbo.cuenta c
            WHERE c.idempresa = @e AND COALESCE(c.activo, true)
              AND (
                EXISTS (
                  SELECT 1 FROM dbo.cuentadocumento d
                  WHERE d.idcuenta = c.id AND COALESCE(d.activo, true)
                    AND regexp_replace(upper(trim(COALESCE(d.numerodocumento, ''))), '[^0-9A-Z]', '', 'g') = @doc
                )
                OR EXISTS (
                  SELECT 1
                  FROM dbo.usuarioecommerce u
                  JOIN dbo.usuarioecommercecuenta l ON l.idusuarioecommerce = u.id
                  WHERE l.idcuenta = c.id
                    AND u.idempresa = @e
                    AND regexp_replace(upper(trim(COALESCE(u.numerodocumento, ''))), '[^0-9A-Z]', '', 'g') = @doc
                )
              )
            ORDER BY c.id
            LIMIT 5
            """, conn);
        cmd.Parameters.AddWithValue("e", idEmpresa);
        cmd.Parameters.AddWithValue("doc", docNorm);
        return await ReadIdsAsync(cmd, ct);
    }

    private static async Task<long[]> ReadIdsAsync(NpgsqlCommand cmd, CancellationToken ct)
    {
        var ids = new List<long>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            ids.Add(r.GetInt64(0));
        return ids.ToArray();
    }

    private static string? ReadString(NpgsqlDataReader r, int i) =>
        r.IsDBNull(i) ? null : r.GetValue(i)?.ToString();

    private static long? ReadLongNull(NpgsqlDataReader r, int i) =>
        r.IsDBNull(i) ? null : Convert.ToInt64(r.GetValue(i));

    private static decimal? ReadDecimalNull(NpgsqlDataReader r, int i) =>
        r.IsDBNull(i) ? null : Convert.ToDecimal(r.GetValue(i));

    private static string? Trunc(string? s, int max) =>
        string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s[..max]);
}
