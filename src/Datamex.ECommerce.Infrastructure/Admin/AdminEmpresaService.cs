using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Application.Integration;
using Datamex.ECommerce.Application.Sync;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Domain.Enums;
using Datamex.ECommerce.Infrastructure.Persistence;
using Datamex.ECommerce.Infrastructure.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Datamex.ECommerce.Infrastructure.Admin;

public sealed class AdminEmpresaService : IAdminEmpresaService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IErpConnectionResolver _erpCs;
    private readonly IPedidoErpMigrationService _migration;
    private readonly ErpSecretBox _secrets;

    public AdminEmpresaService(
        EcomDbContext db,
        ITenantContext tenant,
        IErpConnectionResolver erpCs,
        IPedidoErpMigrationService migration,
        IConfiguration config)
    {
        _db = db;
        _tenant = tenant;
        _erpCs = erpCs;
        _migration = migration;
        _secrets = new ErpSecretBox(config);
    }

    private long EmpresaId => _tenant.IDEmpresa
        ?? throw new InvalidOperationException("Tenant no resuelto.");

    public async Task<AdminEmpresaConfigDto?> GetConfigAsync(CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var emp = await _db.Empresas.AsNoTracking()
            .FirstOrDefaultAsync(e => e.ID == idEmpresa, ct);
        if (emp is null) return null;

        var cfg = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct);
        if (cfg is null) return null;

        var redes = await _db.EmpresaConfiguracionEcommerceRedSociales.AsNoTracking()
            .Where(x => x.IDEmpresaConfiguracionEcommerce == idEmpresa && x.Activo)
            .OrderBy(x => x.NombreRedSocial)
            .Select(x => new AdminEmpresaRedSocialDto(
                x.ID, x.NombreRedSocial, x.DireccionURLRedSocial, x.LogoRedSocial, x.Activo))
            .ToListAsync(ct);

        return await MapConfigAsync(emp, cfg, redes, ct);
    }

    public async Task<AdminEmpresaConfigDto> UpdateConfigAsync(
        UpdateEmpresaConfigRequest request, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var emp = await _db.Empresas.FirstOrDefaultAsync(e => e.ID == idEmpresa, ct)
            ?? throw new InvalidOperationException("Empresa no encontrada.");
        var cfg = await _db.EmpresaConfiguracionesEcommerce.FirstOrDefaultAsync(c => c.ID == idEmpresa, ct)
            ?? throw new InvalidOperationException("Configuración e-commerce no encontrada.");

        var now = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.RazonSocial))
            emp.RazonSocial = request.RazonSocial.Trim();
        if (request.NombreFantasia is not null)
            emp.NombreFantasia = NullIfEmpty(request.NombreFantasia);
        if (request.NumeroDocumento is not null)
            emp.NumeroDocumento = NullIfEmpty(request.NumeroDocumento);

        if (request.SlugTienda is not null)
        {
            var slug = request.SlugTienda.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("SlugTienda no puede quedar vacío.");
            var taken = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
                .AnyAsync(c => c.SlugTienda == slug && c.ID != idEmpresa, ct);
            if (taken)
                throw new InvalidOperationException($"El slug '{slug}' ya está en uso.");
            cfg.SlugTienda = slug;
        }

        if (request.DominioCustom is not null)
            cfg.DominioCustom = NullIfEmpty(request.DominioCustom);
        if (request.RealizaDelivery.HasValue) cfg.RealizaDelivery = request.RealizaDelivery.Value;
        if (request.RealizaPickup.HasValue) cfg.RealizaPickup = request.RealizaPickup.Value;
        if (request.CobraEnLaEntrega.HasValue) cfg.CobraEnLaEntrega = request.CobraEnLaEntrega.Value;
        if (request.CobraAlHacerPedido.HasValue) cfg.CobraAlHacerPedido = request.CobraAlHacerPedido.Value;
        if (request.MontoPedidoMinimo.HasValue) cfg.MontoPedidoMinimo = Math.Max(0, request.MontoPedidoMinimo.Value);
        if (request.MontoUmbralMayorista.HasValue) cfg.MontoUmbralMayorista = Math.Max(0, request.MontoUmbralMayorista.Value);
        if (request.UsarListaPrecioCuentaErp.HasValue) cfg.UsarListaPrecioCuentaErp = request.UsarListaPrecioCuentaErp.Value;
        if (request.IDListaPrecioMayoristaDefault.HasValue)
            cfg.IDListaPrecioMayoristaDefault = request.IDListaPrecioMayoristaDefault.Value == 0
                ? null
                : request.IDListaPrecioMayoristaDefault;
        if (request.ProcesaPedidosPorPlanilla.HasValue) cfg.ProcesaPedidosPorPlanilla = request.ProcesaPedidosPorPlanilla.Value;
        if (request.ProcesaPedidosERP.HasValue) cfg.ProcesaPedidosERP = request.ProcesaPedidosERP.Value;
        // Exclusión mutua ERP ↔ planilla (como switch ERP Site)
        if (request.ProcesaPedidosERP == true) cfg.ProcesaPedidosPorPlanilla = false;
        if (request.ProcesaPedidosPorPlanilla == true) cfg.ProcesaPedidosERP = false;
        if (request.ControlaStockEnCarrito.HasValue) cfg.ControlaStockEnCarrito = request.ControlaStockEnCarrito.Value;
        if (!string.IsNullOrWhiteSpace(request.ModoIntegracion))
        {
            if (!TenantIntegrationMode.TryParse(request.ModoIntegracion, out var mode))
                throw new ArgumentException("ModoIntegracion inválido (use Integrated, Standalone o External).");
            cfg.ModoIntegracion = (int)mode;
            // Integrated: MutatesLocalStock=false; la vitrina valida contra espejo ERP.
            // No forzar ControlaStockEnCarrito=false (ese flag viene del ERP / admin).
        }
        if (request.IDDeposito.HasValue && request.IDDeposito.Value > 0) cfg.IDDeposito = request.IDDeposito.Value;
        if (request.IDArticuloDelivery.HasValue) cfg.IDArticuloDelivery = request.IDArticuloDelivery.Value <= 0 ? null : request.IDArticuloDelivery;

        if (request.DireccionURLFacebook is not null) cfg.DireccionURLFacebook = NullIfEmpty(request.DireccionURLFacebook);
        if (request.DireccionURLTwitter is not null) cfg.DireccionURLTwitter = NullIfEmpty(request.DireccionURLTwitter);
        if (request.DireccionURLInstagram is not null) cfg.DireccionURLInstagram = NullIfEmpty(request.DireccionURLInstagram);
        if (request.DireccionURLYoutube is not null) cfg.DireccionURLYoutube = NullIfEmpty(request.DireccionURLYoutube);
        if (request.DireccionURLWhatsapp is not null) cfg.DireccionURLWhatsapp = NullIfEmpty(request.DireccionURLWhatsapp);

        if (request.ClavePublicaProcesadoraTarjeta is not null)
            cfg.ClavePublicaProcesadoraTarjeta = NullIfEmpty(request.ClavePublicaProcesadoraTarjeta);
        if (!string.IsNullOrWhiteSpace(request.ClavePrivadaProcesadoraTarjeta))
            cfg.ClavePrivadaProcesadoraTarjeta = request.ClavePrivadaProcesadoraTarjeta.Trim();
        if (request.DireccionURLCheckout is not null)
            cfg.DireccionURLCheckout = NullIfEmpty(request.DireccionURLCheckout);
        if (request.CodigoComercio.HasValue)
            cfg.CodigoComercio = request.CodigoComercio.Value <= 0 ? null : request.CodigoComercio;
        if (request.CodigoSucursal.HasValue)
            cfg.CodigoSucursal = request.CodigoSucursal.Value <= 0 ? null : request.CodigoSucursal;

        if (request.CodEmpresa.HasValue) emp.CodEmpresa = request.CodEmpresa.Value;
        if (request.DireccionEmpresa is not null) emp.DireccionEmpresa = NullIfEmpty(request.DireccionEmpresa);
        if (request.Telefono is not null) emp.Telefono = NullIfEmpty(request.Telefono);
        if (request.Fax is not null) emp.Fax = NullIfEmpty(request.Fax);
        if (request.EMail is not null) emp.EMail = NullIfEmpty(request.EMail);
        if (request.RepresentanteLegal is not null) emp.RepresentanteLegal = NullIfEmpty(request.RepresentanteLegal);
        if (request.IDPais.HasValue) emp.IDPais = request.IDPais.Value <= 0 ? null : request.IDPais;
        if (request.LogoEmpresa is not null) emp.LogoEmpresa = NullIfEmpty(request.LogoEmpresa);

        if (request.TiempoAproxEntregaPickup is not null)
            cfg.TiempoAproxEntregaPickup = ParseOptionalTime(request.TiempoAproxEntregaPickup, nameof(request.TiempoAproxEntregaPickup));
        if (request.TiempoAproxEntregaDelivery is not null)
            cfg.TiempoAproxEntregaDelivery = ParseOptionalTime(request.TiempoAproxEntregaDelivery, nameof(request.TiempoAproxEntregaDelivery));
        if (request.NombreServidorCorreoElectronico is not null)
            cfg.NombreServidorCorreoElectronico = NullIfEmpty(request.NombreServidorCorreoElectronico);
        if (request.NombreUsuarioCorreoElectronico is not null)
            cfg.NombreUsuarioCorreoElectronico = NullIfEmpty(request.NombreUsuarioCorreoElectronico);
        if (!string.IsNullOrWhiteSpace(request.PasswordCorreoElectronico))
            cfg.PasswordCorreoElectronico = request.PasswordCorreoElectronico.Trim();
        if (request.PuertoCorreoElectronico.HasValue)
            cfg.PuertoCorreoElectronico = request.PuertoCorreoElectronico.Value <= 0 ? null : request.PuertoCorreoElectronico;

        if (request.ContenidoFaqHtml is not null) cfg.ContenidoFaqHtml = NullIfEmpty(request.ContenidoFaqHtml);
        if (request.ContenidoTerminosHtml is not null) cfg.ContenidoTerminosHtml = NullIfEmpty(request.ContenidoTerminosHtml);
        if (request.ContenidoComoComprarHtml is not null) cfg.ContenidoComoComprarHtml = NullIfEmpty(request.ContenidoComoComprarHtml);
        if (request.DeliveryEmiteFactura.HasValue) cfg.DeliveryEmiteFactura = request.DeliveryEmiteFactura.Value;
        if (request.DeliveryEmiteTicket.HasValue) cfg.DeliveryEmiteTicket = request.DeliveryEmiteTicket.Value;
        if (request.PickupEmiteFactura.HasValue) cfg.PickupEmiteFactura = request.PickupEmiteFactura.Value;
        if (request.PickupEmiteTicket.HasValue) cfg.PickupEmiteTicket = request.PickupEmiteTicket.Value;
        if (request.CreditoDesarrolladoPor is not null) cfg.CreditoDesarrolladoPor = NullIfEmpty(request.CreditoDesarrolladoPor);
        if (request.CreditoDesarrolladoPorUrl is not null) cfg.CreditoDesarrolladoPorUrl = NullIfEmpty(request.CreditoDesarrolladoPorUrl);

        if (request.SyncStockContinuo.HasValue)
            cfg.SyncStockContinuo = request.SyncStockContinuo.Value;
        if (request.SyncStockIntervaloMinutos.HasValue)
            cfg.SyncStockIntervaloMinutos = Math.Clamp(request.SyncStockIntervaloMinutos.Value, 1, 120);
        if (request.MigrarPedidosAlGenerar.HasValue)
        {
            cfg.MigrarPedidosAlGenerar = request.MigrarPedidosAlGenerar.Value;
            // Migrar al generar implica procesar pedidos ERP (write-back).
            if (cfg.MigrarPedidosAlGenerar)
            {
                cfg.ProcesaPedidosERP = true;
                cfg.ProcesaPedidosPorPlanilla = false;
            }
        }

        if (request.ClearErpConnection == true)
        {
            cfg.ErpHost = null;
            cfg.ErpPort = null;
            cfg.ErpDatabase = null;
            cfg.ErpUsername = null;
            cfg.ErpPasswordProtected = null;
            cfg.ErpSslMode = null;
        }
        else
        {
            if (request.ErpHost is not null) cfg.ErpHost = NullIfEmpty(request.ErpHost);
            if (request.ErpPort.HasValue) cfg.ErpPort = request.ErpPort.Value <= 0 ? null : request.ErpPort;
            if (request.ErpDatabase is not null) cfg.ErpDatabase = NullIfEmpty(request.ErpDatabase);
            if (request.ErpUsername is not null) cfg.ErpUsername = NullIfEmpty(request.ErpUsername);
            if (request.ErpSslMode is not null) cfg.ErpSslMode = NullIfEmpty(request.ErpSslMode);
            if (request.ClearErpPassword == true)
                cfg.ErpPasswordProtected = null;
            else if (!string.IsNullOrWhiteSpace(request.ErpPassword))
                cfg.ErpPasswordProtected = _secrets.Protect(request.ErpPassword.Trim());
        }

        if (request.ActivarIntegracionErpCompleta == true)
            ApplyIntegracionCompleta(cfg);

        if (request.ErpAllowWriteBack.HasValue)
            cfg.ErpAllowWriteBack = request.ErpAllowWriteBack.Value;
        else if (!string.IsNullOrWhiteSpace(request.ErpPassword)
                 || !string.IsNullOrWhiteSpace(request.ErpHost))
        {
            // Al configurar conexión, por defecto habilitar write-back de pedidos.
            if (!string.IsNullOrWhiteSpace(cfg.ErpHost)
                && !string.IsNullOrWhiteSpace(cfg.ErpDatabase)
                && !string.IsNullOrWhiteSpace(cfg.ErpUsername))
                ApplyIntegracionCompleta(cfg);
        }

        emp.UltModificacion = now;
        emp.UserName = "admin";
        cfg.UltModificacion = now;
        cfg.UserName = "admin";
        await _db.SaveChangesAsync(ct);

        var redes = await _db.EmpresaConfiguracionEcommerceRedSociales.AsNoTracking()
            .Where(x => x.IDEmpresaConfiguracionEcommerce == idEmpresa && x.Activo)
            .OrderBy(x => x.NombreRedSocial)
            .Select(x => new AdminEmpresaRedSocialDto(
                x.ID, x.NombreRedSocial, x.DireccionURLRedSocial, x.LogoRedSocial, x.Activo))
            .ToListAsync(ct);

        return await MapConfigAsync(emp, cfg, redes, ct);
    }

    public async Task<IReadOnlyList<AdminDepositoDto>> ListDepositosAsync(CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        return await _db.Depositos.AsNoTracking()
            .Where(d => d.IDEmpresa == idEmpresa && d.Activo)
            .OrderByDescending(d => d.EsDepositoEcommerce)
            .ThenBy(d => d.CodDeposito)
            .Select(d => new AdminDepositoDto(d.ID, d.CodDeposito, d.DescripcionDeposito, d.EsDepositoEcommerce))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AdminAgenciaDto>> ListAgenciasAsync(CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        return await _db.Agencias.AsNoTracking()
            .Where(a => a.IDEmpresa == idEmpresa)
            .OrderBy(a => a.CodAgencia)
            .Select(a => new AdminAgenciaDto(
                a.ID, a.CodAgencia, a.DescripcionAgencia, a.Direccion,
                a.IDCiudad, a.IDDeposito, a.Latitud, a.Longitud,
                a.EsSucursalEcommerce, a.Activo))
            .ToListAsync(ct);
    }

    public async Task<AdminAgenciaDto> CreateAgenciaAsync(UpsertAgenciaRequest request, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        ValidateAgencia(request);
        var now = DateTime.UtcNow;

        var cod = request.CodAgencia ?? ((await _db.Agencias.AsNoTracking()
            .Where(a => a.IDEmpresa == idEmpresa)
            .MaxAsync(a => (int?)a.CodAgencia, ct)) ?? 0) + 1;

        if (await _db.Agencias.AnyAsync(a => a.IDEmpresa == idEmpresa && a.CodAgencia == cod, ct))
            throw new InvalidOperationException($"Ya existe CodAgencia={cod}.");

        var a = new Agencia
        {
            IDEmpresa = idEmpresa,
            Activo = request.Activo,
            UltModificacion = now,
            UserName = "admin",
            CodAgencia = cod,
            DescripcionAgencia = request.DescripcionAgencia.Trim(),
            Direccion = NullIfEmpty(request.Direccion),
            IDCiudad = request.IDCiudad,
            IDDeposito = request.IDDeposito,
            Latitud = request.Latitud,
            Longitud = request.Longitud,
            EsSucursalEcommerce = request.EsSucursalEcommerce
        };
        _db.Agencias.Add(a);
        await _db.SaveChangesAsync(ct);
        return MapAgencia(a);
    }

    public async Task<AdminAgenciaDto> UpdateAgenciaAsync(long id, UpsertAgenciaRequest request, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        ValidateAgencia(request);
        var a = await _db.Agencias.FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Agencia no encontrada.");

        if (request.CodAgencia is int cod && cod != a.CodAgencia)
        {
            if (await _db.Agencias.AnyAsync(x => x.IDEmpresa == idEmpresa && x.CodAgencia == cod && x.ID != id, ct))
                throw new InvalidOperationException($"Ya existe CodAgencia={cod}.");
            a.CodAgencia = cod;
        }

        a.DescripcionAgencia = request.DescripcionAgencia.Trim();
        a.Direccion = NullIfEmpty(request.Direccion);
        a.IDCiudad = request.IDCiudad;
        a.IDDeposito = request.IDDeposito;
        a.Latitud = request.Latitud;
        a.Longitud = request.Longitud;
        a.EsSucursalEcommerce = request.EsSucursalEcommerce;
        a.Activo = request.Activo;
        a.UltModificacion = DateTime.UtcNow;
        a.UserName = "admin";
        await _db.SaveChangesAsync(ct);
        return MapAgencia(a);
    }

    public async Task DeleteAgenciaAsync(long id, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var a = await _db.Agencias.FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Agencia no encontrada.");

        a.Activo = false;
        a.EsSucursalEcommerce = false;
        a.UltModificacion = DateTime.UtcNow;
        a.UserName = "admin";

        var horarios = await _db.HorariosAtencion
            .Where(h => h.IDEmpresa == idEmpresa && h.IDAgencia == id && h.Activo)
            .ToListAsync(ct);
        foreach (var h in horarios)
        {
            h.Activo = false;
            h.UltModificacion = DateTime.UtcNow;
            h.UserName = "admin";
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AdminHorarioDto>> ListHorariosAsync(long? idAgencia = null, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var q =
            from h in _db.HorariosAtencion.AsNoTracking()
            join a in _db.Agencias.AsNoTracking() on h.IDAgencia equals a.ID
            where h.IDEmpresa == idEmpresa
            select new { h, a.DescripcionAgencia };

        if (idAgencia is > 0)
            q = q.Where(x => x.h.IDAgencia == idAgencia);

        var rows = await q
            .OrderBy(x => x.h.IDAgencia)
            .ThenBy(x => x.h.DiaSemana)
            .ToListAsync(ct);

        return rows.Select(x => MapHorario(x.h, x.DescripcionAgencia)).ToList();
    }

    public async Task<AdminHorarioDto> CreateHorarioAsync(UpsertHorarioRequest request, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var (ap1, ci1, ap2, ci2) = ParseHorario(request);
        await EnsureAgenciaAsync(idEmpresa, request.IDAgencia, ct);
        if (request.DiaSemana > 6)
            throw new ArgumentException("DiaSemana debe ser 0–6 (Dom–Sáb).");

        var exists = await _db.HorariosAtencion.AnyAsync(h =>
            h.IDEmpresa == idEmpresa
            && h.IDAgencia == request.IDAgencia
            && h.DiaSemana == request.DiaSemana
            && h.Activo, ct);
        if (exists)
            throw new InvalidOperationException("Ya hay un horario activo para esa agencia y día.");

        var h = new HorarioAtencion
        {
            IDEmpresa = idEmpresa,
            Activo = request.Activo,
            UltModificacion = DateTime.UtcNow,
            UserName = "admin",
            IDAgencia = request.IDAgencia,
            DiaSemana = request.DiaSemana,
            HoraPrimeraApertura = ap1,
            HoraPrimerCierre = ci1,
            HoraSegundaApertura = ap2,
            HoraSegundoCierre = ci2
        };
        _db.HorariosAtencion.Add(h);
        await _db.SaveChangesAsync(ct);

        var nombre = await _db.Agencias.AsNoTracking()
            .Where(a => a.ID == h.IDAgencia)
            .Select(a => a.DescripcionAgencia)
            .FirstAsync(ct);
        return MapHorario(h, nombre);
    }

    public async Task<AdminHorarioDto> UpdateHorarioAsync(long id, UpsertHorarioRequest request, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var (ap1, ci1, ap2, ci2) = ParseHorario(request);
        await EnsureAgenciaAsync(idEmpresa, request.IDAgencia, ct);
        if (request.DiaSemana > 6)
            throw new ArgumentException("DiaSemana debe ser 0–6 (Dom–Sáb).");

        var h = await _db.HorariosAtencion.FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Horario no encontrado.");

        var clash = await _db.HorariosAtencion.AnyAsync(x =>
            x.IDEmpresa == idEmpresa
            && x.IDAgencia == request.IDAgencia
            && x.DiaSemana == request.DiaSemana
            && x.Activo
            && x.ID != id, ct);
        if (clash)
            throw new InvalidOperationException("Ya hay un horario activo para esa agencia y día.");

        h.IDAgencia = request.IDAgencia;
        h.DiaSemana = request.DiaSemana;
        h.HoraPrimeraApertura = ap1;
        h.HoraPrimerCierre = ci1;
        h.HoraSegundaApertura = ap2;
        h.HoraSegundoCierre = ci2;
        h.Activo = request.Activo;
        h.UltModificacion = DateTime.UtcNow;
        h.UserName = "admin";
        await _db.SaveChangesAsync(ct);

        var nombre = await _db.Agencias.AsNoTracking()
            .Where(a => a.ID == h.IDAgencia)
            .Select(a => a.DescripcionAgencia)
            .FirstAsync(ct);
        return MapHorario(h, nombre);
    }

    public async Task DeleteHorarioAsync(long id, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var h = await _db.HorariosAtencion.FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Horario no encontrado.");
        h.Activo = false;
        h.UltModificacion = DateTime.UtcNow;
        h.UserName = "admin";
        await _db.SaveChangesAsync(ct);
    }

    private async Task EnsureAgenciaAsync(long idEmpresa, long idAgencia, CancellationToken ct)
    {
        var ok = await _db.Agencias.AnyAsync(a => a.ID == idAgencia && a.IDEmpresa == idEmpresa && a.Activo, ct);
        if (!ok) throw new InvalidOperationException("Agencia no encontrada o inactiva.");
    }

    private static void ValidateAgencia(UpsertAgenciaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DescripcionAgencia))
            throw new ArgumentException("DescripcionAgencia requerida.");
    }

    private static (TimeSpan, TimeSpan, TimeSpan, TimeSpan) ParseHorario(UpsertHorarioRequest request)
    {
        var ap1 = ParseTime(request.HoraPrimeraApertura, "HoraPrimeraApertura");
        var ci1 = ParseTime(request.HoraPrimerCierre, "HoraPrimerCierre");
        if (ci1 <= ap1)
            throw new ArgumentException("HoraPrimerCierre debe ser posterior a HoraPrimeraApertura.");

        var ap2 = string.IsNullOrWhiteSpace(request.HoraSegundaApertura)
            ? TimeSpan.Zero
            : ParseTime(request.HoraSegundaApertura, "HoraSegundaApertura");
        var ci2 = string.IsNullOrWhiteSpace(request.HoraSegundoCierre)
            ? TimeSpan.Zero
            : ParseTime(request.HoraSegundoCierre, "HoraSegundoCierre");

        if (ap2 != TimeSpan.Zero || ci2 != TimeSpan.Zero)
        {
            if (ci2 <= ap2)
                throw new ArgumentException("HoraSegundoCierre debe ser posterior a HoraSegundaApertura.");
            if (ap2 < ci1)
                throw new ArgumentException("La segunda franja debe empezar después del primer cierre.");
        }

        return (ap1, ci1, ap2, ci2);
    }

    private static TimeSpan? ParseOptionalTime(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return ParseTime(value.Trim(), field);
    }

    private static TimeSpan ParseTime(string value, string field)
    {
        if (!TimeSpan.TryParse(value, out var ts))
            throw new ArgumentException($"{field} inválida (use HH:mm).");
        if (ts < TimeSpan.Zero || ts >= TimeSpan.FromDays(1))
            throw new ArgumentException($"{field} fuera de rango.");
        return ts;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Fmt(TimeSpan t) => t.ToString(@"hh\:mm");

    private static string? FmtOpt(TimeSpan? t) => t is null ? null : Fmt(t.Value);

    private static void ApplyIntegracionCompleta(EmpresaConfiguracionEcommerce cfg)
    {
        cfg.ErpAllowWriteBack = true;
        cfg.ProcesaPedidosERP = true;
        cfg.ProcesaPedidosPorPlanilla = false;
        cfg.MigrarPedidosAlGenerar = true;
        cfg.SyncStockContinuo = true;
        cfg.SyncStockIntervaloMinutos = 1;
        cfg.ControlaStockEnCarrito = false;
        cfg.ModoIntegracion = (int)IntegrationMode.Integrated;
    }

    private async Task<AdminEmpresaConfigDto> MapConfigAsync(
        Empresa emp,
        EmpresaConfiguracionEcommerce cfg,
        IReadOnlyList<AdminEmpresaRedSocialDto>? redes,
        CancellationToken ct)
    {
        var source = await _erpCs.ResolveSourceAsync(emp.ID, ct);
        var writeBack = await _erpCs.AllowWriteBackAsync(emp.ID, ct);
        var own = !string.IsNullOrWhiteSpace(cfg.ErpHost)
                  && !string.IsNullOrWhiteSpace(cfg.ErpDatabase)
                  && !string.IsNullOrWhiteSpace(cfg.ErpUsername);
        return new(
            emp.ID, emp.RazonSocial, emp.NombreFantasia, emp.NumeroDocumento,
            cfg.SlugTienda, cfg.DominioCustom,
            cfg.RealizaDelivery, cfg.RealizaPickup, cfg.CobraEnLaEntrega, cfg.CobraAlHacerPedido,
            cfg.MontoPedidoMinimo, cfg.ProcesaPedidosPorPlanilla, cfg.ProcesaPedidosERP,
            cfg.ControlaStockEnCarrito,
            ((IntegrationMode)(cfg.ModoIntegracion is >= 0 and <= 2 ? cfg.ModoIntegracion : TenantIntegrationMode.DefaultModo)).ToString(),
            cfg.IDDeposito, cfg.IDArticuloDelivery,
            cfg.DireccionURLFacebook, cfg.DireccionURLTwitter, cfg.DireccionURLInstagram,
            cfg.DireccionURLYoutube, cfg.DireccionURLWhatsapp,
            cfg.ClavePublicaProcesadoraTarjeta,
            !string.IsNullOrWhiteSpace(cfg.ClavePrivadaProcesadoraTarjeta),
            cfg.DireccionURLCheckout,
            cfg.CodigoComercio,
            cfg.CodigoSucursal,
            cfg.MontoUmbralMayorista,
            cfg.UsarListaPrecioCuentaErp,
            cfg.IDListaPrecioMayoristaDefault,
            emp.CodEmpresa,
            emp.DireccionEmpresa,
            emp.Telefono,
            emp.Fax,
            emp.EMail,
            emp.RepresentanteLegal,
            emp.IDPais,
            emp.LogoEmpresa,
            FmtOpt(cfg.TiempoAproxEntregaPickup),
            FmtOpt(cfg.TiempoAproxEntregaDelivery),
            cfg.NombreServidorCorreoElectronico,
            cfg.NombreUsuarioCorreoElectronico,
            !string.IsNullOrWhiteSpace(cfg.PasswordCorreoElectronico),
            cfg.PuertoCorreoElectronico,
            cfg.NroUltPedidoCarritoCompra,
            cfg.UltIDProcesoNegocio,
            redes,
            cfg.ContenidoFaqHtml,
            cfg.ContenidoTerminosHtml,
            cfg.ContenidoComoComprarHtml,
            cfg.DeliveryEmiteFactura,
            cfg.DeliveryEmiteTicket,
            cfg.PickupEmiteFactura,
            cfg.PickupEmiteTicket,
            cfg.CreditoDesarrolladoPor,
            cfg.CreditoDesarrolladoPorUrl,
            cfg.SyncStockContinuo,
            cfg.MigrarPedidosAlGenerar,
            cfg.SyncStockIntervaloMinutos,
            cfg.ErpHost,
            cfg.ErpPort,
            cfg.ErpDatabase,
            cfg.ErpUsername,
            !string.IsNullOrWhiteSpace(cfg.ErpPasswordProtected),
            cfg.ErpSslMode,
            cfg.ErpAllowWriteBack,
            own,
            source,
            writeBack);
    }

    public Task<ErpConnectionTestResult> TestErpConnectionAsync(
        TestErpConnectionRequest? draft = null, CancellationToken ct = default) =>
        _erpCs.TestAsync(EmpresaId, draft, ct);

    public async Task<AdminErpHealthDto> GetErpHealthAsync(CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var cfg = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ID == idEmpresa, ct)
            ?? throw new InvalidOperationException("Configuración e-commerce no encontrada.");

        var source = await _erpCs.ResolveSourceAsync(idEmpresa, ct);
        var hasCs = source is not "None";
        var writeBack = await _erpCs.AllowWriteBackAsync(idEmpresa, ct);
        var modo = ((IntegrationMode)(cfg.ModoIntegracion is >= 0 and <= 2
            ? cfg.ModoIntegracion
            : TenantIntegrationMode.DefaultModo)).ToString();

        var stockCursor = await _db.SyncCursors.AsNoTracking()
            .Where(x => x.IDEmpresa == idEmpresa && x.EntityKey == "stock")
            .Select(x => new { x.LastRunUtc, x.LastStatus, x.RowsProcessed, x.LastError })
            .FirstOrDefaultAsync(ct);

        var pending = await _db.PedidosMovil.AsNoTracking()
            .CountAsync(p => p.IDEmpresa == idEmpresa
                             && p.Activo
                             && !p.Migrado
                             && p.OrigenPedido == OrigenPedidoMovil.Ecommerce, ct);

        string? hint = null;
        if (!hasCs)
            hint = "Configurá la conexión ERP de esta empresa (o el fallback global).";
        else if (modo != nameof(IntegrationMode.Integrated))
            hint = "Pasá a modo Integrated para usar el backoffice ERP y este admin a la vez (stock espejo + pedidos).";
        else if (!cfg.SyncStockContinuo)
            hint = "Activá lectura continua de stock para near real-time.";
        else if (!writeBack || !cfg.MigrarPedidosAlGenerar)
            hint = "Activá write-back + migrar al generar para que los pedidos caigan en ERP.";
        else if (pending > 0)
            hint = $"{pending} pedido(s) pendientes — usá “Reintentar pendientes” o esperá el job.";
        else if (stockCursor?.LastStatus == "error")
            hint = "Último sync de stock con error — usá “Traer stock ahora” o Probá conexión.";
        else
            hint = "Integración operativa: ambos backoffices (ERP Site y este admin) + stock continuo + pedidos a ERP.";

        var interval = Math.Clamp(cfg.SyncStockIntervaloMinutos <= 0 ? 5 : cfg.SyncStockIntervaloMinutos, 1, 120);
        DateTime? nextDue = null;
        var dueNow = false;
        int? secondsUntil = null;
        var now = DateTime.UtcNow;
        if (cfg.SyncStockContinuo && hasCs)
        {
            if (stockCursor?.LastRunUtc is DateTime last)
            {
                nextDue = last.AddMinutes(interval);
                var rem = (int)Math.Ceiling((nextDue.Value - now).TotalSeconds);
                if (rem <= 0)
                {
                    dueNow = true;
                    secondsUntil = 0;
                }
                else
                {
                    secondsUntil = rem;
                }
            }
            else
            {
                dueNow = true;
                nextDue = now;
                secondsUntil = 0;
            }
        }

        return new AdminErpHealthDto(
            hasCs,
            source,
            writeBack,
            cfg.SyncStockContinuo,
            cfg.SyncStockIntervaloMinutos,
            cfg.MigrarPedidosAlGenerar,
            cfg.ProcesaPedidosERP,
            modo,
            cfg.IDDeposito,
            stockCursor?.LastRunUtc,
            stockCursor?.LastStatus,
            stockCursor?.RowsProcessed ?? 0,
            stockCursor?.LastError,
            pending,
            hint,
            nextDue,
            secondsUntil,
            dueNow);
    }

    public async Task<MigrarPedidosResult> FlushPendingOrdersAsync(CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        if (!await _erpCs.AllowWriteBackAsync(idEmpresa, ct))
            throw new InvalidOperationException("Write-back ERP no habilitado para esta empresa.");

        var ids = await _db.PedidosMovil.AsNoTracking()
            .Where(p => p.IDEmpresa == idEmpresa
                        && p.Activo
                        && !p.Migrado
                        && p.OrigenPedido == OrigenPedidoMovil.Ecommerce)
            .OrderBy(p => p.ID)
            .Select(p => p.ID)
            .Take(40)
            .ToListAsync(ct);

        if (ids.Count == 0)
            return new MigrarPedidosResult(0, 0, 0, Array.Empty<MigracionPedidoLogDto>());

        return await _migration.MigrarAsync(ids, ct);
    }

    private static AdminAgenciaDto MapAgencia(Agencia a) => new(
        a.ID, a.CodAgencia, a.DescripcionAgencia, a.Direccion,
        a.IDCiudad, a.IDDeposito, a.Latitud, a.Longitud,
        a.EsSucursalEcommerce, a.Activo);

    private static AdminHorarioDto MapHorario(HorarioAtencion h, string? nombre) => new(
        h.ID, h.IDAgencia, nombre, h.DiaSemana,
        Fmt(h.HoraPrimeraApertura), Fmt(h.HoraPrimerCierre),
        Fmt(h.HoraSegundaApertura), Fmt(h.HoraSegundoCierre),
        h.Activo);
}
