using System.Text.RegularExpressions;
using Datamex.ECommerce.Application.Options;
using Datamex.ECommerce.Application.Provisioning;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Domain.Enums;
using Datamex.ECommerce.Infrastructure.Auth;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Datamex.ECommerce.Infrastructure.Provisioning;

/// <summary>
/// Provisiona empresa ecom sobre tablas espejo ERP.
/// Manual y FromErp comparten el mismo bootstrap de maestros.
/// </summary>
public sealed class TenantProvisioningService : ITenantProvisioningService
{
    private readonly EcomDbContext _db;
    private readonly ECommerceOptions _options;

    public TenantProvisioningService(EcomDbContext db, IOptions<ECommerceOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<TenantSummaryDto>> ListAsync(CancellationToken ct = default)
    {
        var rows = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .Join(_db.Empresas.AsNoTracking(), c => c.ID, e => e.ID, (c, e) => new { c, e })
            .OrderBy(x => x.e.RazonSocial)
            .ToListAsync(ct);

        return rows.Select(x => MapSummary(x.e, x.c)).ToList();
    }

    public async Task<TenantSummaryDto?> GetAsync(long idEmpresa, CancellationToken ct = default)
    {
        var e = await _db.Empresas.AsNoTracking().FirstOrDefaultAsync(x => x.ID == idEmpresa, ct);
        var c = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == idEmpresa, ct);
        if (e is null || c is null)
            return null;
        return MapSummary(e, c);
    }

    public async Task<ProvisionResultDto> ProvisionManualAsync(
        ManualTenantRequest request, CancellationToken ct = default)
    {
        ValidateSlug(request.SlugTienda);
        if (string.IsNullOrWhiteSpace(request.RazonSocial))
            throw new ArgumentException("Razón social obligatoria.");
        if (string.IsNullOrWhiteSpace(request.NumeroDocumento))
            throw new ArgumentException("Documento / RUC obligatorio.");

        var slug = request.SlugTienda.Trim().ToLowerInvariant();
        if (await _db.EmpresaConfiguracionesEcommerce.AnyAsync(c => c.SlugTienda == slug, ct))
            throw new InvalidOperationException($"Slug '{slug}' ya está en uso.");

        var now = DateTime.UtcNow;
        var empresa = new Empresa
        {
            Activo = true,
            UltModificacion = now,
            UserName = "provision-manual",
            RazonSocial = request.RazonSocial.Trim(),
            NombreFantasia = string.IsNullOrWhiteSpace(request.NombreFantasia)
                ? request.RazonSocial.Trim()
                : request.NombreFantasia.Trim(),
            NumeroDocumento = request.NumeroDocumento.Trim()
        };
        _db.Empresas.Add(empresa);
        await _db.SaveChangesAsync(ct);

        var config = new EmpresaConfiguracionEcommerce
        {
            ID = empresa.ID,
            Activo = true,
            UltModificacion = now,
            UserName = "provision-manual",
            RealizaDelivery = request.RealizaDelivery,
            RealizaPickup = request.RealizaPickup,
            ControlaStockEnCarrito = request.ControlaStockEnCarrito,
            MontoPedidoMinimo = request.MontoPedidoMinimo,
            // Standalone: no procesa en ERP hasta que se integre
            ProcesaPedidosERP = false,
            SlugTienda = slug,
            DominioCustom = NullIfEmpty(request.DominioCustom)
        };
        _db.EmpresaConfiguracionesEcommerce.Add(config);
        await _db.SaveChangesAsync(ct);

        await EnsureGeographyIfEmptyAsync(now, ct);
        await BootstrapTenantDefaultsAsync(empresa.ID, config, createAdmin: true,
            adminUsuario: request.AdminUsuario,
            adminPassword: request.AdminPassword,
            adminEmail: request.AdminEmail,
            ct);

        var adminUser = string.IsNullOrWhiteSpace(request.AdminUsuario)
            ? "admin"
            : request.AdminUsuario.Trim();
        var adminPass = string.IsNullOrWhiteSpace(request.AdminPassword)
            ? "Admin12!"
            : request.AdminPassword;

        return new ProvisionResultDto(
            empresa.ID,
            slug,
            ProvisioningSource.Manual,
            $"Empresa creada en modo Standalone (mismas tablas ERP). Slug={slug}.",
            adminUser,
            adminPass);
    }

    public async Task<ProvisionResultDto> ProvisionFromErpAsync(
        ErpTenantRequest request, CancellationToken ct = default)
    {
        ValidateSlug(request.SlugTienda);
        if (request.IDEmpresa <= 0)
            throw new ArgumentException("IDEmpresa del ERP es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.RazonSocial))
            throw new ArgumentException("Razón social obligatoria.");

        var slug = request.SlugTienda.Trim().ToLowerInvariant();
        var slugTaken = await _db.EmpresaConfiguracionesEcommerce.AnyAsync(
            c => c.SlugTienda == slug && c.ID != request.IDEmpresa, ct);
        if (slugTaken)
            throw new InvalidOperationException($"Slug '{slug}' ya está en uso por otra empresa.");

        var now = DateTime.UtcNow;
        var empresa = await _db.Empresas.FirstOrDefaultAsync(e => e.ID == request.IDEmpresa, ct);
        if (empresa is null)
        {
            // ID fijo = IDEmpresa del ERP (no autoincrement distinto). Mismo PK que ERP.
            await _db.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO empresa (ID, Activo, UltModificacion, UserName, RazonSocial, NombreFantasia, NumeroDocumento)
VALUES ({request.IDEmpresa}, {true}, {now}, {"provision-erp"}, {request.RazonSocial.Trim()},
        {(request.NombreFantasia ?? request.RazonSocial).Trim()}, {request.NumeroDocumento ?? string.Empty});
", ct);
            _db.ChangeTracker.Clear();
            empresa = await _db.Empresas.FirstAsync(e => e.ID == request.IDEmpresa, ct);
        }
        else
        {
            empresa.RazonSocial = request.RazonSocial.Trim();
            empresa.NombreFantasia = string.IsNullOrWhiteSpace(request.NombreFantasia)
                ? empresa.NombreFantasia
                : request.NombreFantasia.Trim();
            empresa.NumeroDocumento = request.NumeroDocumento?.Trim() ?? empresa.NumeroDocumento;
            empresa.Activo = true;
            empresa.UltModificacion = now;
            empresa.UserName = "provision-erp";
        }

        var config = await _db.EmpresaConfiguracionesEcommerce
            .FirstOrDefaultAsync(c => c.ID == request.IDEmpresa, ct);
        if (config is null)
        {
            config = new EmpresaConfiguracionEcommerce
            {
                ID = request.IDEmpresa,
                Activo = true,
                UltModificacion = now,
                UserName = "provision-erp",
                IDDeposito = request.IDDepositoErp ?? 0,
                IDArticuloDelivery = request.IDArticuloDeliveryErp
            };
            _db.EmpresaConfiguracionesEcommerce.Add(config);
        }

        config.Activo = true;
        config.UltModificacion = now;
        config.UserName = "provision-erp";
        config.SlugTienda = slug;
        config.DominioCustom = NullIfEmpty(request.DominioCustom);
        config.RealizaDelivery = request.RealizaDelivery;
        config.RealizaPickup = request.RealizaPickup;
        config.ControlaStockEnCarrito = request.ControlaStockEnCarrito;
        config.ProcesaPedidosERP = request.ProcesaPedidosERP;
        config.MigrarPedidosAlGenerar = request.ProcesaPedidosERP;
        config.SyncStockContinuo = true;
        config.SyncStockIntervaloMinutos = 5;
        config.MontoPedidoMinimo = request.MontoPedidoMinimo;
        config.ClavePublicaProcesadoraTarjeta = request.ClavePublicaProcesadoraTarjeta;
        config.ClavePrivadaProcesadoraTarjeta = request.ClavePrivadaProcesadoraTarjeta;
        config.DireccionURLCheckout = request.DireccionURLCheckout;
        config.CodigoComercio = request.CodigoComercio;
        config.CodigoSucursal = request.CodigoSucursal;
        if (request.IDDepositoErp.HasValue)
            config.IDDeposito = request.IDDepositoErp.Value;
        if (request.IDArticuloDeliveryErp.HasValue)
            config.IDArticuloDelivery = request.IDArticuloDeliveryErp;

        await _db.SaveChangesAsync(ct);

        await EnsureGeographyIfEmptyAsync(now, ct);

        if (request.CreateLocalDefaultsIfMissing)
        {
            await BootstrapTenantDefaultsAsync(
                request.IDEmpresa, config, createAdmin: false,
                adminUsuario: null, adminPassword: null, adminEmail: null, ct);
        }

        return new ProvisionResultDto(
            request.IDEmpresa,
            slug,
            ProvisioningSource.FromErp,
            $"Empresa {request.IDEmpresa} provisionada desde ERP (mismas tablas). ProcesaPedidosERP={config.ProcesaPedidosERP}.",
            null,
            null);
    }

    private async Task BootstrapTenantDefaultsAsync(
        long idEmpresa,
        EmpresaConfiguracionEcommerce config,
        bool createAdmin,
        string? adminUsuario,
        string? adminPassword,
        string? adminEmail,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Tipos documento (espejo tipodocumento)
        if (!await _db.TiposDocumento.AnyAsync(t => t.IDEmpresa == idEmpresa, ct))
        {
            _db.TiposDocumento.AddRange(
                new TipoDocumento
                {
                    IDEmpresa = idEmpresa, Activo = true, UltModificacion = now, UserName = "provision",
                    CodTipoDocumento = 1, DescripcionTipoDocumento = "RUC", EsRuc = true
                },
                new TipoDocumento
                {
                    IDEmpresa = idEmpresa, Activo = true, UltModificacion = now, UserName = "provision",
                    CodTipoDocumento = 2, DescripcionTipoDocumento = "Cédula de Identidad", EsRuc = false
                });
            await _db.SaveChangesAsync(ct);
        }

        // Tipo depósito Normal + depósito ecom
        var tipo = await _db.TiposDeposito
            .FirstOrDefaultAsync(t => t.IDEmpresa == idEmpresa && t.Normal, ct);
        if (tipo is null)
        {
            tipo = new TipoDeposito
            {
                IDEmpresa = idEmpresa, Activo = true, UltModificacion = now, UserName = "provision",
                CodTipoDeposito = 1, DescripcionTipoDeposito = "Normal / Facturable",
                Normal = true, Consolidado = true, Facturable = true
            };
            _db.TiposDeposito.Add(tipo);
            await _db.SaveChangesAsync(ct);
        }

        var deposito = await _db.Depositos
            .FirstOrDefaultAsync(d => d.IDEmpresa == idEmpresa && d.EsDepositoEcommerce, ct);
        if (deposito is null && config.IDDeposito > 0)
        {
            deposito = await _db.Depositos
                .FirstOrDefaultAsync(d => d.ID == config.IDDeposito && d.IDEmpresa == idEmpresa, ct);
        }
        if (deposito is null)
        {
            deposito = new Deposito
            {
                IDEmpresa = idEmpresa, Activo = true, UltModificacion = now, UserName = "provision",
                CodDeposito = 1,
                IDTipoDeposito = tipo.ID,
                DescripcionDeposito = "Depósito Ecom",
                DepositoEntrega = true,
                EsDepositoEcommerce = true
            };
            _db.Depositos.Add(deposito);
            await _db.SaveChangesAsync(ct);
        }

        config.IDDeposito = deposito.ID;
        config.UltModificacion = now;

        // Listas precio B2C / B2B
        if (!await _db.ListasPrecio.AnyAsync(l => l.IDEmpresa == idEmpresa && l.EsListaPrecioEcommerce, ct))
        {
            _db.ListasPrecio.AddRange(
                new ListaPrecio
                {
                    IDEmpresa = idEmpresa, Activo = true, UltModificacion = now,
                    NroListaPrecio = 1, Descripcion = "Lista Ecom B2C",
                    EsListaPrecioEcommerce = true, PrecioConImpuestos = true,
                    FechaRegistro = now, FechaInicioVigencia = now.AddYears(-1), CanalEcommerce = "B2C",
                    Estado = 2
                },
                new ListaPrecio
                {
                    IDEmpresa = idEmpresa, Activo = true, UltModificacion = now,
                    NroListaPrecio = 2, Descripcion = "Lista Ecom B2B",
                    EsListaPrecioEcommerce = true, PrecioConImpuestos = true,
                    FechaRegistro = now, FechaInicioVigencia = now.AddYears(-1), CanalEcommerce = "B2B",
                    Estado = 2
                });
            await _db.SaveChangesAsync(ct);
        }

        // Agencia default (pickup / mapa)
        var ciudad = await _db.Ciudades.AsNoTracking().OrderBy(c => c.ID).FirstOrDefaultAsync(ct);
        if (!await _db.Agencias.AnyAsync(a => a.IDEmpresa == idEmpresa, ct))
        {
            _db.Agencias.Add(new Agencia
            {
                IDEmpresa = idEmpresa, Activo = true, UltModificacion = now, UserName = "provision",
                CodAgencia = 1,
                DescripcionAgencia = "Casa Central",
                Direccion = "Pendiente",
                IDCiudad = ciudad?.ID,
                IDDeposito = deposito.ID,
                Latitud = _options.DefaultMapLat,
                Longitud = _options.DefaultMapLng,
                EsSucursalEcommerce = true
            });
            await _db.SaveChangesAsync(ct);
        }

        // Artículo delivery + config.IDArticuloDelivery
        var delivery = await _db.Articulos
            .FirstOrDefaultAsync(a => a.IDEmpresa == idEmpresa && a.SKU == "SKU-DELIVERY", ct);
        if (delivery is null && config.IDArticuloDelivery is null)
        {
            delivery = new Articulo
            {
                IDEmpresa = idEmpresa, Activo = true, ActivoEcommerce = false,
                UltModificacion = now, UserName = "provision",
                NroArticulo = 9001, SKU = "SKU-DELIVERY",
                DescripcionArticulo = "Costo de Delivery",
                DescripcionArticuloAbreviada = "Delivery",
                DescripcionArticuloExtensa = "Servicio delivery (IDArticuloDelivery ERP).",
                IDMarca = 1, NombreMarca = "Sistema", EstadoEcommerce = "oculto"
            };
            _db.Articulos.Add(delivery);
            await _db.SaveChangesAsync(ct);
            _db.ArticuloEmbalajes.Add(new ArticuloEmbalaje
            {
                IDEmpresa = idEmpresa, IDArticulo = delivery.ID, Activo = true,
                UltModificacion = now, CantidadxEmbalaje = 1,
                DescripcionEmbalaje = "Unidad", CodigoBarras = "DELIVERY-1", PrecioDemo = 15000m
            });
            await _db.SaveChangesAsync(ct);
            config.IDArticuloDelivery = delivery.ID;
        }
        else if (delivery is not null)
        {
            config.IDArticuloDelivery ??= delivery.ID;
        }

        // Tarifas demo ligadas a barrios maestros (si no hay)
        if (config.RealizaDelivery
            && !await _db.TarifasDelivery.AnyAsync(t => t.IDEmpresa == idEmpresa, ct)
            && await _db.Barrios.AnyAsync(ct))
        {
            var agencia = await _db.Agencias.FirstAsync(a => a.IDEmpresa == idEmpresa, ct);
            var barrioRows = await (
                from b in _db.Barrios
                join d in _db.Distritos on b.IDDistrito equals d.ID
                join l in _db.Localidades on d.IDLocalidad equals l.ID
                join c in _db.Ciudades on l.IDCiudad equals c.ID
                where b.Activo
                select new { Barrio = b, Ciudad = c }
            ).Take(5).ToListAsync(ct);

            foreach (var row in barrioRows)
            {
                _db.TarifasDelivery.Add(new TarifaDelivery
                {
                    IDEmpresa = idEmpresa, Activo = true, UltModificacion = now, UserName = "provision",
                    IDDivisionPolitica = row.Ciudad.IDDivisionPolitica,
                    IDCiudad = row.Ciudad.ID,
                    IDBarrio = row.Barrio.ID,
                    NombreCiudad = row.Ciudad.DescripcionCiudad,
                    NombreBarrio = row.Barrio.DescripcionBarrio,
                    ImporteDelivery = 15000m + row.Barrio.CodBarrio * 500m,
                    FechaInicioVigencia = now.Date.AddYears(-1),
                    FechaFinVigencia = now.Date.AddYears(5),
                    Estado = 1, // autorizado / vigente en storefront
                    IDAgencia = agencia.ID
                });
            }
        }

        // TiendaEcommerce token
        if (!await _db.TiendasEcommerce.AnyAsync(t => t.IDEmpresa == idEmpresa, ct))
        {
            _db.TiendasEcommerce.Add(new TiendaEcommerce
            {
                IDEmpresa = idEmpresa, Activo = true, UltModificacion = now,
                IDClasificacionArticulo = 1,
                TokenTienda = $"tok-{idEmpresa}-{Guid.NewGuid():N}"[..12],
                IDDepositoTienda = deposito.ID,
                Nombre = config.SlugTienda ?? $"Tienda {idEmpresa}",
                HabilitaB2C = true,
                HabilitaB2B = true
            });
        }

        if (createAdmin)
        {
            var userName = string.IsNullOrWhiteSpace(adminUsuario) ? "admin" : adminUsuario.Trim();
            var pass = string.IsNullOrWhiteSpace(adminPassword) ? "Admin12!" : adminPassword;
            var email = string.IsNullOrWhiteSpace(adminEmail)
                ? $"{userName}@{config.SlugTienda}.local"
                : adminEmail.Trim();

            if (!await _db.UsuariosEcommerce.AnyAsync(u => u.IDEmpresa == idEmpresa && u.Usuario == userName, ct))
            {
                var ci = await _db.TiposDocumento
                    .FirstAsync(t => t.IDEmpresa == idEmpresa && t.CodTipoDocumento == 2, ct);
                var usuario = new UsuarioEcommerce
                {
                    IDEmpresa = idEmpresa, Activo = true, Activado = true, Bloqueado = false,
                    UltModificacion = now, UserName = "provision",
                    CodigoUsuario = "1",
                    Usuario = userName,
                    Password = $"plain:{pass}",
                    Nombre = "Admin",
                    Apellido = "Tienda",
                    Email = email,
                    IDTipoDocumento = ci.ID,
                    NumeroDocumento = "0",
                    NumeroTelefono = "021000000",
                    NumeroMovil = "0981000000",
                    FechaRegistro = now,
                    CanalPreferido = "B2C",
                    CheckSum = JwtAuthService.HashPassword(pass)
                };
                _db.UsuariosEcommerce.Add(usuario);
                await _db.SaveChangesAsync(ct);

                var cuenta = new Cuenta
                {
                    IDEmpresa = idEmpresa, Activo = true, UltModificacion = now, UserName = "provision",
                    NroCuenta = 1,
                    RazonSocial = "ADMIN TIENDA",
                    Email = email,
                    IDCiudad = ciudad?.ID,
                    IDDivisionPolitica = ciudad?.IDDivisionPolitica,
                    IDAgencia = await _db.Agencias.Where(a => a.IDEmpresa == idEmpresa)
                        .Select(a => (long?)a.ID).FirstOrDefaultAsync(ct),
                    Latitud = _options.DefaultMapLat,
                    Longitud = _options.DefaultMapLng
                };
                _db.Cuentas.Add(cuenta);
                await _db.SaveChangesAsync(ct);

                _db.UsuariosEcommerceCuenta.Add(new UsuarioEcommerceCuenta
                {
                    IDEmpresa = idEmpresa,
                    IDUsuarioEcommerce = usuario.ID,
                    IDCuenta = cuenta.ID,
                    EsPrincipal = true,
                    Activo = true,
                    UltModificacion = now
                });
            }

            // ---- Backoffice RBAC (manager) ----
            // Rol super: con funcionalidad "manager:all" para permitir acceso total.
            const string superPerm = "manager:all";
            const string superRolCodigo = "SUPER";

            var superRol = await _db.AdminRoles.FirstOrDefaultAsync(
                r => r.IDEmpresa == idEmpresa && r.CodigoRol == superRolCodigo, ct);
            if (superRol is null)
            {
                superRol = new AdminRol
                {
                    IDEmpresa = idEmpresa,
                    CodigoRol = superRolCodigo,
                    NombreRol = "Super Admin",
                    Activo = true,
                    UltModificacion = now,
                    UserName = "provision"
                };
                _db.AdminRoles.Add(superRol);
                await _db.SaveChangesAsync(ct);
            }

            var permAll = await _db.AdminFuncionalidades.FirstOrDefaultAsync(
                f => f.IDEmpresa == idEmpresa && f.CodigoPermission == superPerm, ct);
            if (permAll is null)
            {
                permAll = new AdminFuncionalidad
                {
                    IDEmpresa = idEmpresa,
                    CodigoPermission = superPerm,
                    Descripcion = "Acceso total manager",
                    Activo = true,
                    UltModificacion = now
                };
                _db.AdminFuncionalidades.Add(permAll);
                await _db.SaveChangesAsync(ct);
            }

            var adminUsuarioRow = await _db.AdminUsuarios.FirstOrDefaultAsync(
                u => u.IDEmpresa == idEmpresa && u.Usuario == userName, ct);
            if (adminUsuarioRow is null)
            {
                adminUsuarioRow = new AdminUsuario
                {
                    IDEmpresa = idEmpresa,
                    Usuario = userName,
                    PasswordHash = JwtAuthService.HashPassword(pass),
                    Nombre = "Admin",
                    Email = email,
                    Activo = true,
                    UltModificacion = now,
                    UserName = "provision"
                };
                _db.AdminUsuarios.Add(adminUsuarioRow);
                await _db.SaveChangesAsync(ct);
            }

            var userHasSuperRole = await _db.AdminUsuariosRoles.AnyAsync(
                ur => ur.IDEmpresa == idEmpresa && ur.IDUsuario == adminUsuarioRow.ID && ur.IDRol == superRol.ID,
                ct);
            if (!userHasSuperRole)
            {
                _db.AdminUsuariosRoles.Add(new AdminUsuarioRol
                {
                    IDEmpresa = idEmpresa,
                    IDUsuario = adminUsuarioRow.ID,
                    IDRol = superRol.ID,
                    Activo = true,
                    UltModificacion = now,
                    UserName = "provision"
                });
                await _db.SaveChangesAsync(ct);
            }

            var roleHasSuperPerm = await _db.AdminRolFuncionalidades.AnyAsync(
                rf => rf.IDEmpresa == idEmpresa && rf.IDRol == superRol.ID && rf.IDFuncionalidad == permAll.ID,
                ct);
            if (!roleHasSuperPerm)
            {
                _db.AdminRolFuncionalidades.Add(new AdminRolFuncionalidad
                {
                    IDEmpresa = idEmpresa,
                    IDRol = superRol.ID,
                    IDFuncionalidad = permAll.ID,
                    Activo = true,
                    UltModificacion = now,
                    UserName = "provision"
                });
                await _db.SaveChangesAsync(ct);
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task EnsureGeographyIfEmptyAsync(DateTime now, CancellationToken ct)
    {
        if (await _db.Ciudades.AnyAsync(ct))
            return;

        var div = new DivisionPolitica
        {
            Activo = true, UltModificacion = now, UserName = "provision",
            CodDivisionPolitica = 1, DescripcionDivisionPolitica = "Central",
            NombreAbreviado = "CENT", IDPais = 1
        };
        _db.DivisionesPoliticas.Add(div);
        await _db.SaveChangesAsync(ct);

        var asu = new Ciudad
        {
            Activo = true, UltModificacion = now, UserName = "provision",
            CodCiudad = 1, DescripcionCiudad = "Asunción", IDDivisionPolitica = div.ID
        };
        _db.Ciudades.Add(asu);
        await _db.SaveChangesAsync(ct);

        var loc = new Localidad
        {
            Activo = true, UltModificacion = now, UserName = "provision",
            CodLocalidad = 1, DescripcionLocalidad = "Loc Asunción", IDCiudad = asu.ID
        };
        _db.Localidades.Add(loc);
        await _db.SaveChangesAsync(ct);

        var dist = new Distrito
        {
            Activo = true, UltModificacion = now, UserName = "provision",
            CodDistrito = 1, DescripcionDistrito = "Dist Asunción", IDLocalidad = loc.ID
        };
        _db.Distritos.Add(dist);
        await _db.SaveChangesAsync(ct);

        foreach (var (cod, name) in new[] { (1, "Centro"), (2, "Villa Morra"), (3, "Las Mercedes") })
        {
            _db.Barrios.Add(new Barrio
            {
                Activo = true, UltModificacion = now, UserName = "provision",
                CodBarrio = cod, DescripcionBarrio = name, IDDistrito = dist.ID
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    private TenantSummaryDto MapSummary(Empresa e, EmpresaConfiguracionEcommerce c)
    {
        var source = c.ProcesaPedidosERP ? ProvisioningSource.FromErp : ProvisioningSource.Manual;
        return new TenantSummaryDto(
            e.ID, e.RazonSocial, e.NombreFantasia, e.NumeroDocumento,
            c.SlugTienda, c.DominioCustom, c.Activo && e.Activo,
            c.ProcesaPedidosERP, c.RealizaDelivery, c.RealizaPickup,
            c.ControlaStockEnCarrito, c.IDDeposito, c.IDArticuloDelivery,
            source, _options.IntegrationMode);
    }

    private static void ValidateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("SlugTienda obligatorio.");
        if (!Regex.IsMatch(slug.Trim(), @"^[a-z0-9]([a-z0-9\-]{1,48}[a-z0-9])?$", RegexOptions.IgnoreCase))
            throw new ArgumentException("Slug inválido (letras, números y guiones; 3–50).");
    }

    private static string? NullIfEmpty(string? v) =>
        string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
