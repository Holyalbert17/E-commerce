using Datamex.ECommerce.Application.Sync;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Datamex.ECommerce.Infrastructure.Sync;

public sealed class ErpConnectionResolver : IErpConnectionResolver
{
    private readonly EcomDbContext _db;
    private readonly IConfiguration _config;
    private readonly ErpSecretBox _secrets;
    private readonly ILogger<ErpConnectionResolver> _log;

    public ErpConnectionResolver(
        EcomDbContext db,
        IConfiguration config,
        ILogger<ErpConnectionResolver> log)
    {
        _db = db;
        _config = config;
        _secrets = new ErpSecretBox(config);
        _log = log;
    }

    public async Task<string?> ResolveAsync(long idEmpresa, CancellationToken ct = default)
    {
        var cfg = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);

        var tenantCs = BuildFromParts(
            cfg?.ErpHost, cfg?.ErpPort, cfg?.ErpDatabase, cfg?.ErpUsername,
            UnprotectPassword(cfg?.ErpPasswordProtected), cfg?.ErpSslMode);
        if (!string.IsNullOrWhiteSpace(tenantCs))
            return tenantCs;

        return NullIfEmpty(_config.GetConnectionString("Erp"));
    }

    public async Task<string> ResolveSourceAsync(long idEmpresa, CancellationToken ct = default)
    {
        var cfg = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);
        if (HasOwnCs(cfg)) return "Tenant";
        if (!string.IsNullOrWhiteSpace(_config.GetConnectionString("Erp"))) return "Global";
        return "None";
    }

    public async Task<bool> AllowWriteBackAsync(long idEmpresa, CancellationToken ct = default)
    {
        var global = _config.GetValue("Erp:AllowWriteBack", false);
        var cfg = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);
        if (cfg is null) return global;

        if (cfg.ErpAllowWriteBack) return true;
        if (global) return true;
        return HasOwnCs(cfg) && cfg.ProcesaPedidosERP;
    }

    public async Task<ErpConnectionTestResult> TestAsync(
        long idEmpresa,
        TestErpConnectionRequest? draft = null,
        CancellationToken ct = default)
    {
        var cfg = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);

        string? cs;
        string source;
        string? host;
        string? database;

        if (draft is not null && HasDraftCs(draft))
        {
            var pwd = draft.ErpPassword;
            if (string.IsNullOrWhiteSpace(pwd) && draft.UseSavedPasswordIfEmpty)
                pwd = UnprotectPassword(cfg?.ErpPasswordProtected);
            cs = BuildFromParts(
                draft.ErpHost, draft.ErpPort, draft.ErpDatabase, draft.ErpUsername,
                pwd, draft.ErpSslMode);
            source = "Draft";
            host = NullIfEmpty(draft.ErpHost);
            database = NullIfEmpty(draft.ErpDatabase);
        }
        else if (HasOwnCs(cfg))
        {
            cs = BuildFromParts(
                cfg!.ErpHost, cfg.ErpPort, cfg.ErpDatabase, cfg.ErpUsername,
                UnprotectPassword(cfg.ErpPasswordProtected), cfg.ErpSslMode);
            source = "Tenant";
            host = cfg.ErpHost;
            database = cfg.ErpDatabase;
        }
        else
        {
            cs = NullIfEmpty(_config.GetConnectionString("Erp"));
            source = string.IsNullOrWhiteSpace(cs) ? "None" : "Global";
            host = TryPeekHost(cs);
            database = TryPeekDatabase(cs);
        }

        if (string.IsNullOrWhiteSpace(cs))
            return new ErpConnectionTestResult(
                false,
                "Sin connection string ERP (configurá host/db/usuario o ConnectionStrings:Erp).",
                Source: source);

        var idDeposito = cfg?.IDDeposito > 0 ? cfg.IDDeposito : 1L;

        try
        {
            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync(ct);

            string dbName;
            string ver;
            await using (var cmd = new NpgsqlCommand("SELECT current_database(), version()", conn))
            await using (var r = await cmd.ExecuteReaderAsync(ct))
            {
                if (!await r.ReadAsync(ct))
                    return new ErpConnectionTestResult(false, "Conectó pero no hubo respuesta.", Source: source, Host: host);
                dbName = r.GetString(0);
                ver = r.GetString(1);
            }

            var major = ParsePgMajor(ver);

            bool? canEmpresa = null;
            bool? canStock = null;
            long? stockRows = null;

            try
            {
                await using var cmd = new NpgsqlCommand(
                    """
                    SELECT EXISTS(
                      SELECT 1 FROM dbo.empresa WHERE id = @e AND activo = true LIMIT 1)
                    """, conn);
                cmd.Parameters.AddWithValue("e", idEmpresa);
                canEmpresa = (bool)(await cmd.ExecuteScalarAsync(ct) ?? false);
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Probe empresa falló (tabla distinta?).");
                canEmpresa = null;
            }

            try
            {
                await using var cmd = new NpgsqlCommand(
                    """
                    SELECT COUNT(*)::bigint
                    FROM dbo.stock
                    WHERE idempresa = @e AND iddeposito = @d AND activo = true
                    """, conn);
                cmd.Parameters.AddWithValue("e", idEmpresa);
                cmd.Parameters.AddWithValue("d", idDeposito);
                stockRows = Convert.ToInt64(await cmd.ExecuteScalarAsync(ct) ?? 0L);
                canStock = true;
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Probe stock falló.");
                canStock = false;
            }

            var hints = new List<string> { $"OK · DB={dbName} · {ver.Split(',')[0]}" };
            hints.Add($"origen={source}");
            if (canEmpresa == false)
                hints.Add($"⚠ no hay empresa #{idEmpresa} activa en ese ERP");
            else if (canEmpresa == true)
                hints.Add($"empresa #{idEmpresa} OK");
            if (canStock == true)
                hints.Add($"stock depósito #{idDeposito}: {stockRows} filas");
            else if (canStock == false)
                hints.Add("⚠ no se pudo leer dbo.stock");

            return new ErpConnectionTestResult(
                true,
                string.Join(" · ", hints),
                major,
                source,
                host,
                dbName,
                canEmpresa,
                canStock,
                stockRows,
                idDeposito);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Test ERP falló empresa={Empresa} source={Source}", idEmpresa, source);
            return new ErpConnectionTestResult(false, ex.Message, Source: source, Host: host, Database: database);
        }
    }

    internal string? ProtectPassword(string? plain) =>
        string.IsNullOrWhiteSpace(plain) ? null : _secrets.Protect(plain.Trim());

    internal string? UnprotectPassword(string? stored) => _secrets.Unprotect(stored);

    private static bool HasOwnCs(Domain.Entities.EmpresaConfiguracionEcommerce? cfg) =>
        cfg is not null
        && !string.IsNullOrWhiteSpace(cfg.ErpHost)
        && !string.IsNullOrWhiteSpace(cfg.ErpDatabase)
        && !string.IsNullOrWhiteSpace(cfg.ErpUsername);

    private static bool HasDraftCs(TestErpConnectionRequest d) =>
        !string.IsNullOrWhiteSpace(d.ErpHost)
        && !string.IsNullOrWhiteSpace(d.ErpDatabase)
        && !string.IsNullOrWhiteSpace(d.ErpUsername);

    private static string? BuildFromParts(
        string? host, int? port, string? database, string? username, string? password, string? sslMode)
    {
        if (string.IsNullOrWhiteSpace(host)
            || string.IsNullOrWhiteSpace(database)
            || string.IsNullOrWhiteSpace(username))
            return null;

        var p = port is > 0 ? port.Value : 5432;
        var ssl = string.IsNullOrWhiteSpace(sslMode) ? "Prefer" : sslMode.Trim();
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host.Trim(),
            Port = p,
            Database = database.Trim(),
            Username = username.Trim(),
            Password = password ?? "",
            SslMode = Enum.TryParse<SslMode>(ssl, true, out var mode) ? mode : SslMode.Prefer,
            Timeout = 15,
            CommandTimeout = 60,
            SearchPath = "dbo, public"
        };
        return builder.ConnectionString;
    }

    private static int ParsePgMajor(string ver)
    {
        if (!ver.StartsWith("PostgreSQL ", StringComparison.OrdinalIgnoreCase)) return 0;
        var part = ver.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (part.Length > 1 && int.TryParse(part[1].Split('.')[0], out var m)) return m;
        return 0;
    }

    private static string? TryPeekHost(string? cs)
    {
        if (string.IsNullOrWhiteSpace(cs)) return null;
        try { return new NpgsqlConnectionStringBuilder(cs).Host; }
        catch { return null; }
    }

    private static string? TryPeekDatabase(string? cs)
    {
        if (string.IsNullOrWhiteSpace(cs)) return null;
        try { return new NpgsqlConnectionStringBuilder(cs).Database; }
        catch { return null; }
    }

    private static string? NullIfEmpty(string? v) =>
        string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
