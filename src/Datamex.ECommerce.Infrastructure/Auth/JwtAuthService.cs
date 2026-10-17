using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Auth;
using Datamex.ECommerce.Application.Notifications;
using Datamex.ECommerce.Application.Options;
using Datamex.ECommerce.Application.Sync;
using Datamex.ECommerce.Infrastructure.Sync;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Datamex.ECommerce.Infrastructure.Auth;

public sealed class JwtAuthService : IAuthService
{
    private readonly EcomDbContext _db;
    private readonly ECommerceOptions _options;
    private readonly ITenantContext _tenant;
    private readonly IEmailSender _email;
    private readonly ICuentaErpWriter _cuentaErp;
    private readonly IRegistroClienteErpLookup _registroErp;
    private readonly IConfiguration _config;

    public JwtAuthService(
        EcomDbContext db,
        IOptions<ECommerceOptions> options,
        ITenantContext tenant,
        IEmailSender email,
        ICuentaErpWriter cuentaErp,
        IRegistroClienteErpLookup registroErp,
        IConfiguration config)
    {
        _db = db;
        _options = options.Value;
        _tenant = tenant;
        _email = email;
        _cuentaErp = cuentaErp;
        _registroErp = registroErp;
        _config = config;
    }

    public async Task<TokenResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var idEmpresa = await ResolveEmpresaAsync(request.IDEmpresa, request.TenantSlug, ct);
        if (!idEmpresa.HasValue)
            throw new InvalidOperationException("No se pudo resolver la tienda. Recargá la página e intentá de nuevo.");

        var loginKey = (request.Usuario ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(loginKey) || string.IsNullOrEmpty(request.Password))
            return null;

        var user = await _db.UsuariosEcommerce
            .FirstOrDefaultAsync(u =>
                u.IDEmpresa == idEmpresa.Value
                && (u.Usuario.ToLower() == loginKey
                    || (u.Email != null && u.Email.ToLower() == loginKey)), ct);

        if (user is null)
            return null;

        if (!user.Activo)
            throw new InvalidOperationException("La cuenta está desactivada. Contactá a la tienda.");
        if (user.Bloqueado)
            throw new InvalidOperationException("La cuenta está bloqueada. Contactá a la tienda.");

        if (!VerifyPassword(request.Password, user.Password))
            return null;

        if (!user.Activado)
        {
            if (!await HasStorePurchasesAsync(user, ct))
                throw new InvalidOperationException(
                    "La cuenta todavía no está activada. Revisá el correo y abrí el enlace de activación.");

            user.Activado = true;
            user.CodigoActivacion = string.Empty;
            user.ChecksumActivacion = string.Empty;
            user.UserName = "ecom-login-autoact";
        }

        if (!IsStorefrontSha256(user.Password))
        {
            user.Password = HashPassword(request.Password);
            user.CheckSum = user.Password;
        }

        user.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var expires = DateTime.UtcNow.AddMinutes(_options.JwtExpirationMinutes);
        // Storefront: minorista salvo mayorista aprobado.
        var canal = user.EstadoMayorista == "Aprobado" && user.CanalPreferido == "B2B" ? "B2B" : "B2C";
        var token = CreateToken(user.ID, user.IDEmpresa, user.Usuario, canal, expires);

        return new TokenResponse(
            token,
            expires,
            user.IDEmpresa,
            user.ID,
            $"{user.Nombre} {user.Apellido}".Trim(),
            canal);
    }

    public async Task<SignUpResult> RegisterAsync(SignUpRequest request, CancellationToken ct = default)
    {
        var idEmpresa = await ResolveEmpresaAsync(null, request.TenantSlug, ct)
                        ?? throw new InvalidOperationException("Tenant no resuelto.");

        ValidateSignUp(request);

        var email = request.Email.Trim().ToLowerInvariant();
        var docNorm = RegistroClienteErpLookup.NormalizeDocumento(request.NumeroDocumento);

        var existsEmail = await _db.UsuariosEcommerce.AnyAsync(
            u => u.IDEmpresa == idEmpresa
                 && u.Activo
                 && ((u.Email != null && u.Email.ToLower() == email)
                     || u.Usuario.ToLower() == email), ct);
        if (existsEmail)
            throw new InvalidOperationException("Ya existe un usuario en la tienda con ese correo.");

        var docsLocal = await _db.UsuariosEcommerce.AsNoTracking()
            .Where(u => u.IDEmpresa == idEmpresa && u.Activo && u.NumeroDocumento != null)
            .Select(u => u.NumeroDocumento!)
            .ToListAsync(ct);
        if (docsLocal.Any(d => RegistroClienteErpLookup.NormalizeDocumento(d) == docNorm))
            throw new InvalidOperationException("Ya existe un usuario en la tienda con ese documento/RUC.");

        var tipoOk = await _db.TiposDocumento.AnyAsync(
            t => t.ID == request.IDTipoDocumento && t.IDEmpresa == idEmpresa && t.Activo, ct);
        if (!tipoOk)
            throw new InvalidOperationException("Tipo de documento inválido.");

        var erpMatch = await _registroErp.FindAsync(idEmpresa, email, docNorm, ct);
        if (!string.IsNullOrWhiteSpace(erpMatch.Error))
            throw new InvalidOperationException(erpMatch.Error);

        Cuenta? cuentaErp = null;
        if (erpMatch.IdCuenta is > 0)
        {
            cuentaErp = await _registroErp.ImportCuentaAsync(idEmpresa, erpMatch.IdCuenta.Value, ct)
                        ?? throw new InvalidOperationException(
                            "El cliente ya existe en el ERP pero no se pudieron copiar los datos de facturación. Reintentá o contactá a la empresa.");
        }

        var maxCod = await _db.UsuariosEcommerce.AsNoTracking()
            .Where(u => u.IDEmpresa == idEmpresa)
            .Select(u => u.CodigoUsuario)
            .ToListAsync(ct);
        var nextCod = maxCod
            .Select(c => long.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var clave = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var user = new UsuarioEcommerce
        {
            IDEmpresa = idEmpresa,
            Activo = true,
            Activado = false,
            Bloqueado = false,
            UltModificacion = now,
            UserName = "ecom-registro",
            CodigoUsuario = nextCod.ToString(),
            Usuario = email,
            Password = HashPassword(request.Password),
            Nombre = request.Nombre.Trim(),
            Apellido = request.Apellido.Trim(),
            IDTipoDocumento = request.IDTipoDocumento,
            NumeroDocumento = request.NumeroDocumento.Trim(),
            Email = email,
            NumeroTelefono = request.Telefono.Trim(),
            NumeroMovil = request.Movil.Trim(),
            FechaRegistro = now,
            CodigoActivacion = clave.Replace("-", ""),
            ChecksumActivacion = Md5Hex(clave),
            CanalPreferido = "B2C",
            CheckSum = HashPassword(request.Password)
        };

        await SyncIdentityAsync("usuarioecommerce", ct);
        _db.UsuariosEcommerce.Add(user);
        await _db.SaveChangesAsync(ct);

        if (cuentaErp is not null)
        {
            await SyncIdentityAsync("usuarioecommercecuenta", ct);
            _db.UsuariosEcommerceCuenta.Add(new UsuarioEcommerceCuenta
            {
                IDEmpresa = idEmpresa,
                IDUsuarioEcommerce = user.ID,
                IDCuenta = cuentaErp.ID,
                EsPrincipal = true,
                Activo = true,
                UltModificacion = now,
                UserName = "ecom-registro"
            });
            await _db.SaveChangesAsync(ct);
        }

        var activationUrl =
            $"{_options.StorefrontBaseUrl.TrimEnd('/')}/activar?c={user.CodigoActivacion}&k={user.ChecksumActivacion}&tenant={request.TenantSlug ?? _options.DefaultTenantSlug}";

        var storeName = await GetStoreNameAsync(idEmpresa, ct);
        await _email.SendActivationAsync(new ActivationEmail(email, user.Nombre, activationUrl, storeName), ct);

        return new SignUpResult(
            user.ID,
            email,
            "Registro OK. Revisá tu email para activar la cuenta (en dev el link también se loguea en la API).",
            activationUrl);
    }

    public async Task<ResendActivationResult> ResendActivationAsync(
        ResendActivationRequest request, CancellationToken ct = default)
    {
        var generic = new ResendActivationResult(
            "Si el email existe y la cuenta no está activada, enviamos un nuevo enlace.",
            null);

        var idEmpresa = await ResolveEmpresaAsync(null, request.TenantSlug, ct);
        if (!idEmpresa.HasValue)
            return generic;

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.UsuariosEcommerce
            .FirstOrDefaultAsync(u =>
                u.IDEmpresa == idEmpresa.Value
                && u.Email != null
                && u.Email.ToLower() == email
                && u.Activo
                && !u.Activado, ct);

        if (user is null)
            return generic;

        var clave = Guid.NewGuid().ToString();
        user.CodigoActivacion = clave.Replace("-", "");
        user.ChecksumActivacion = Md5Hex(clave);
        user.UltModificacion = DateTime.UtcNow;
        user.UserName = "ecom-resend-act";
        await _db.SaveChangesAsync(ct);

        var activationUrl =
            $"{_options.StorefrontBaseUrl.TrimEnd('/')}/activar?c={user.CodigoActivacion}&k={user.ChecksumActivacion}&tenant={request.TenantSlug ?? _options.DefaultTenantSlug}";

        var storeName = await GetStoreNameAsync(idEmpresa.Value, ct);
        await _email.SendActivationAsync(new ActivationEmail(email, user.Nombre, activationUrl, storeName), ct);
        return new ResendActivationResult(generic.Mensaje, activationUrl);
    }

    public async Task<TokenResponse?> ActivateAsync(ActivateRequest request, CancellationToken ct = default)
    {
        var idEmpresa = await ResolveEmpresaAsync(null, request.TenantSlug, ct);
        if (!idEmpresa.HasValue)
            return null;

        var user = await _db.UsuariosEcommerce
            .FirstOrDefaultAsync(u =>
                u.IDEmpresa == idEmpresa.Value
                && u.CodigoActivacion == request.Codigo
                && u.ChecksumActivacion == request.Checksum
                && !u.Activado
                && u.Activo, ct);

        if (user is null)
            return null;

        var now = DateTime.UtcNow;

        Cuenta? cuenta = null;
        var linkedId = await _db.UsuariosEcommerceCuenta.AsNoTracking()
            .Where(l => l.IDEmpresa == idEmpresa.Value
                        && l.IDUsuarioEcommerce == user.ID
                        && l.Activo)
            .OrderByDescending(l => l.EsPrincipal)
            .Select(l => (long?)l.IDCuenta)
            .FirstOrDefaultAsync(ct);
        if (linkedId is > 0)
            cuenta = await _db.Cuentas.FirstOrDefaultAsync(c => c.ID == linkedId.Value, ct);

        // Reutilizar cuenta existente por documento (standalone: tabla cuenta).
        if (cuenta is null && user.IDTipoDocumento.HasValue && !string.IsNullOrWhiteSpace(user.NumeroDocumento))
        {
            cuenta = await _db.Cuentas
                .Where(c => c.IDEmpresa == idEmpresa.Value
                            && c.Activo
                            && _db.UsuariosEcommerceCuenta.Any(uc =>
                                uc.IDCuenta == c.ID
                                && _db.UsuariosEcommerce.Any(u =>
                                    u.ID == uc.IDUsuarioEcommerce
                                    && u.NumeroDocumento == user.NumeroDocumento
                                    && u.IDTipoDocumento == user.IDTipoDocumento)))
                .OrderBy(c => c.NroCuenta)
                .FirstOrDefaultAsync(ct);

            // Búsqueda simple: misma razón social no; mejor por email
            cuenta ??= await _db.Cuentas
                .Where(c => c.IDEmpresa == idEmpresa.Value && c.Activo && c.Email == user.Email)
                .OrderBy(c => c.NroCuenta)
                .FirstOrDefaultAsync(ct);
        }

        if (cuenta is null)
        {
            await SyncIdentityAsync("cuenta", ct);
            var maxNro = await _db.Cuentas.AsNoTracking()
                .Where(c => c.IDEmpresa == idEmpresa.Value)
                .MaxAsync(c => (long?)c.NroCuenta, ct) ?? 0;

            var lista = await _db.ListasPrecio.AsNoTracking()
                .Where(l => l.IDEmpresa == idEmpresa.Value && l.EsListaPrecioEcommerce && l.CanalEcommerce == "B2C" && l.Activo)
                .Select(l => (long?)l.ID)
                .FirstOrDefaultAsync(ct);

            var agencia = await _db.Agencias.AsNoTracking()
                .Where(a => a.IDEmpresa == idEmpresa.Value && a.Activo)
                .OrderBy(a => a.CodAgencia)
                .FirstOrDefaultAsync(ct);

            var ciudadId = agencia?.IDCiudad
                           ?? await _db.Ciudades.AsNoTracking().Where(c => c.Activo).Select(c => (long?)c.ID).FirstOrDefaultAsync(ct);

            long? barrioId = null;
            long? divId = null;
            if (ciudadId.HasValue)
            {
                divId = await _db.Ciudades.AsNoTracking()
                    .Where(c => c.ID == ciudadId).Select(c => (long?)c.IDDivisionPolitica).FirstOrDefaultAsync(ct);
                barrioId = await (
                    from b in _db.Barrios.AsNoTracking()
                    join d in _db.Distritos.AsNoTracking() on b.IDDistrito equals d.ID
                    join l in _db.Localidades.AsNoTracking() on d.IDLocalidad equals l.ID
                    where b.Activo && l.IDCiudad == ciudadId
                    select (long?)b.ID
                ).FirstOrDefaultAsync(ct);
            }

            cuenta = new Cuenta
            {
                IDEmpresa = idEmpresa.Value,
                Activo = true,
                UltModificacion = now,
                UserName = "ecom-activar",
                NroCuenta = maxNro + 1,
                RazonSocial = $"{user.Apellido} {user.Nombre}".ToUpperInvariant(),
                NombreCuenta = user.Nombre.ToUpperInvariant(),
                ApellidoCuenta = user.Apellido.ToUpperInvariant(),
                Direccion = string.Empty,
                Email = user.Email?.ToLowerInvariant(),
                IDCiudad = ciudadId,
                IDBarrio = barrioId,
                IDDivisionPolitica = divId,
                IDListaPrecio = lista,
                IDAgencia = agencia?.ID,
                Latitud = agencia?.Latitud,
                Longitud = agencia?.Longitud
            };
            _db.Cuentas.Add(cuenta);
            await _db.SaveChangesAsync(ct);
        }

        // Paridad ERP: cliente nuevo → INSERT; cliente ya migrado del ERP → no duplicar.
        var cuentaYaEnErp = string.Equals(cuenta.UserName, "erp-sync", StringComparison.OrdinalIgnoreCase);
        if (!cuentaYaEnErp)
        {
            var erpCuentaId = await _cuentaErp.EnsureCuentaInErpAsync(cuenta, user, ct);
            var cfgEmpresa = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ID == idEmpresa.Value, ct);
            if (erpCuentaId is null
                && cfgEmpresa?.ProcesaPedidosERP == true
                && _config.GetValue("Erp:AllowWriteBack", false)
                && !string.IsNullOrWhiteSpace(_config.GetConnectionString("Erp")))
            {
                throw new InvalidOperationException(
                    "No se pudo registrar el cliente en el ERP. Verificá la conexión o reintentá la activación.");
            }
        }

        var linkExists = await _db.UsuariosEcommerceCuenta.AnyAsync(
            c => c.IDEmpresa == idEmpresa.Value
                 && c.IDUsuarioEcommerce == user.ID
                 && c.IDCuenta == cuenta.ID, ct);
        if (!linkExists)
        {
            await SyncIdentityAsync("usuarioecommercecuenta", ct);
            var hasPrincipal = await _db.UsuariosEcommerceCuenta.AnyAsync(
                c => c.IDEmpresa == idEmpresa.Value
                     && c.IDUsuarioEcommerce == user.ID
                     && c.Activo
                     && c.EsPrincipal, ct);
            _db.UsuariosEcommerceCuenta.Add(new UsuarioEcommerceCuenta
            {
                IDEmpresa = idEmpresa.Value,
                IDUsuarioEcommerce = user.ID,
                IDCuenta = cuenta.ID,
                EsPrincipal = !hasPrincipal,
                Activo = true,
                UltModificacion = now,
                UserName = "ecom-activar"
            });
        }

        user.Activado = true;
        user.Bloqueado = false;
        user.CodigoActivacion = string.Empty;
        user.ChecksumActivacion = string.Empty;
        user.UltModificacion = now;
        user.UserName = "ecom-activar";
        await _db.SaveChangesAsync(ct);

        var expires = DateTime.UtcNow.AddMinutes(_options.JwtExpirationMinutes);
        var canal = user.EstadoMayorista == "Aprobado" && user.CanalPreferido == "B2B" ? "B2B" : "B2C";
        var token = CreateToken(user.ID, user.IDEmpresa, user.Usuario, canal, expires);
        return new TokenResponse(
            token, expires, user.IDEmpresa, user.ID,
            $"{user.Nombre} {user.Apellido}".Trim(), canal);
    }

    public async Task<ForgotPasswordResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var email = (request.Email ?? "").Trim().ToLowerInvariant();
        var generic = new ForgotPasswordResult(
            "Si el correo existe y la cuenta está activada, te enviamos un enlace para restablecer la contraseña.",
            null);

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return generic;

        var idEmpresa = await ResolveEmpresaAsync(null, request.TenantSlug, ct);
        if (!idEmpresa.HasValue)
            return generic;

        var user = await _db.UsuariosEcommerce
            .FirstOrDefaultAsync(u =>
                u.IDEmpresa == idEmpresa.Value
                && u.Email != null
                && u.Email.ToLower() == email
                && u.Activo
                && u.Activado
                && !u.Bloqueado, ct);

        if (user is null)
            return generic;

        var clave = Guid.NewGuid().ToString();
        user.CodigoResetPassword = clave.Replace("-", "");
        user.ChecksumResetPassword = Md5Hex(clave);
        user.FechaExpiraResetPassword = DateTime.UtcNow.AddHours(2);
        user.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var tenant = request.TenantSlug ?? _options.DefaultTenantSlug;
        var resetUrl =
            $"{_options.StorefrontBaseUrl.TrimEnd('/')}/recuperar?c={user.CodigoResetPassword}&k={user.ChecksumResetPassword}&tenant={tenant}";

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            var storeName = await GetStoreNameAsync(idEmpresa.Value, ct);
            await _email.SendPasswordResetAsync(new PasswordResetEmail(
                user.Email,
                $"{user.Nombre} {user.Apellido}".Trim(),
                resetUrl,
                storeName), ct);
        }

        return new ForgotPasswordResult(generic.Mensaje, resetUrl);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        if (!string.Equals(request.Password, request.PasswordConfirmation, StringComparison.Ordinal))
            throw new ArgumentException("Las contraseñas no coinciden.");
        if (!TestPassword(request.Password))
            throw new ArgumentException(
                "Password: mínimo 5 caracteres, con letra, número y símbolo; sin 3 caracteres iguales seguidos.");

        var idEmpresa = await ResolveEmpresaAsync(null, request.TenantSlug, ct)
                        ?? throw new InvalidOperationException("Tenant no resuelto.");

        var user = await _db.UsuariosEcommerce
            .FirstOrDefaultAsync(u =>
                u.IDEmpresa == idEmpresa
                && u.CodigoResetPassword == request.Codigo
                && u.ChecksumResetPassword == request.Checksum
                && u.Activo, ct)
            ?? throw new InvalidOperationException("Enlace de recuperación inválido o ya usado.");

        if (user.FechaExpiraResetPassword is null || user.FechaExpiraResetPassword < DateTime.UtcNow)
            throw new InvalidOperationException("El enlace de recuperación expiró. Solicitá uno nuevo.");

        user.Password = HashPassword(request.Password);
        user.CheckSum = user.Password;
        user.CodigoResetPassword = null;
        user.ChecksumResetPassword = null;
        user.FechaExpiraResetPassword = null;
        user.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ChangePasswordAsync(long idUsuarioEcommerce, ChangePasswordRequest request, CancellationToken ct = default)
    {
        if (!string.Equals(request.PasswordNueva, request.PasswordConfirmacion, StringComparison.Ordinal))
            throw new ArgumentException("Las contraseñas nuevas no coinciden.");
        if (!TestPassword(request.PasswordNueva))
            throw new ArgumentException(
                "Password: mínimo 5 caracteres, con letra, número y símbolo; sin 3 caracteres iguales seguidos.");

        var idEmpresa = _tenant.IDEmpresa
                        ?? throw new InvalidOperationException("Tenant no resuelto.");

        var user = await _db.UsuariosEcommerce
            .FirstOrDefaultAsync(u => u.ID == idUsuarioEcommerce && u.IDEmpresa == idEmpresa && u.Activo, ct)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        if (!VerifyPassword(request.PasswordActual, user.Password))
            throw new InvalidOperationException("La contraseña actual no es correcta.");

        user.Password = HashPassword(request.PasswordNueva);
        user.CheckSum = user.Password;
        user.UltModificacion = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<long?> ResolveEmpresaAsync(long? idEmpresa, string? slug, CancellationToken ct)
    {
        if (idEmpresa.HasValue)
            return idEmpresa;
        if (_tenant.IDEmpresa.HasValue)
            return _tenant.IDEmpresa;

        async Task<long?> BySlug(string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;
            if (string.Equals(s, "demo", StringComparison.OrdinalIgnoreCase))
                s = _options.DefaultTenantSlug;
            return await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
                .Where(c => c.SlugTienda == s && c.Activo)
                .Select(c => (long?)c.ID)
                .FirstOrDefaultAsync(ct);
        }

        return await BySlug(slug) ?? await BySlug(_options.DefaultTenantSlug);
    }

    private async Task<bool> HasStorePurchasesAsync(UsuarioEcommerce user, CancellationToken ct)
    {
        var cuentaIds = await _db.UsuariosEcommerceCuenta.AsNoTracking()
            .Where(l => l.IDEmpresa == user.IDEmpresa && l.IDUsuarioEcommerce == user.ID && l.Activo)
            .Select(l => l.IDCuenta)
            .ToListAsync(ct);

        if (cuentaIds.Count == 0 && !string.IsNullOrWhiteSpace(user.Email))
        {
            var email = user.Email.Trim().ToLower();
            cuentaIds = await _db.Cuentas.AsNoTracking()
                .Where(c => c.IDEmpresa == user.IDEmpresa && c.Activo && c.Email != null && c.Email.ToLower() == email)
                .Select(c => c.ID)
                .ToListAsync(ct);
        }

        if (cuentaIds.Count == 0)
            return false;

        return await _db.PedidosMovil.AsNoTracking()
            .AnyAsync(p => p.IDEmpresa == user.IDEmpresa && cuentaIds.Contains(p.IDCuenta), ct);
    }

    private async Task<string> GetStoreNameAsync(long idEmpresa, CancellationToken ct)
    {
        var name = await _db.EmpresaConfiguracionesEcommerce.AsNoTracking()
            .Where(c => c.ID == idEmpresa)
            .Select(c => c.NombreTiendaDisplay)
            .FirstOrDefaultAsync(ct);
        return string.IsNullOrWhiteSpace(name) ? "Datamex" : name.Trim();
    }

    private async Task SyncIdentityAsync(string table, CancellationToken ct)
    {
        // Whitelist only — raw SQL with table name (EF can't parameterize identifiers).
#pragma warning disable EF1002
        var sql = table switch
        {
            "usuarioecommerce" =>
                """
                SELECT setval(
                  pg_get_serial_sequence('dbo.usuarioecommerce', 'ID'),
                  GREATEST((SELECT COALESCE(MAX("ID"), 1) FROM dbo.usuarioecommerce), 1),
                  true)
                """,
            "usuarioecommercecuenta" =>
                """
                SELECT setval(
                  pg_get_serial_sequence('dbo.usuarioecommercecuenta', 'ID'),
                  GREATEST((SELECT COALESCE(MAX("ID"), 1) FROM dbo.usuarioecommercecuenta), 1),
                  true)
                """,
            "cuenta" =>
                """
                SELECT setval(
                  pg_get_serial_sequence('dbo.cuenta', 'ID'),
                  GREATEST((SELECT COALESCE(MAX("ID"), 1) FROM dbo.cuenta), 1),
                  true)
                """,
            _ => throw new ArgumentOutOfRangeException(nameof(table))
        };
        await _db.Database.ExecuteSqlRawAsync(sql, ct);
#pragma warning restore EF1002
    }

    private static void ValidateSignUp(SignUpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length < 2)
            throw new ArgumentException("Nombre inválido.");
        if (string.IsNullOrWhiteSpace(request.Apellido) || request.Apellido.Trim().Length < 2)
            throw new ArgumentException("Apellido inválido.");
        if (string.IsNullOrWhiteSpace(request.NumeroDocumento) || request.NumeroDocumento.Trim().Length < 6)
            throw new ArgumentException("Documento inválido.");
        if (string.IsNullOrWhiteSpace(request.Telefono) || request.Telefono.Trim().Length < 9)
            throw new ArgumentException("Teléfono inválido.");
        if (string.IsNullOrWhiteSpace(request.Movil) || request.Movil.Trim().Length < 9)
            throw new ArgumentException("Móvil inválido.");
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            throw new ArgumentException("Email inválido.");
        if (request.Email.Trim().Length > 100)
            throw new ArgumentException("Email demasiado largo (máx. 100 caracteres).");
        if (!string.Equals(request.Password, request.PasswordConfirmation, StringComparison.Ordinal))
            throw new ArgumentException("Las contraseñas no coinciden.");
        if (!TestPassword(request.Password))
            throw new ArgumentException(
                "Password: mínimo 5 caracteres, con letra, número y símbolo; sin 3 caracteres iguales seguidos.");
    }

    /// <summary>Misma regla que BaseController.TestPassword del ERP.</summary>
    public static bool TestPassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 5 || password.Length > 30)
            return false;
        var hasNumber = new Regex(@"[0-9]+");
        var hasLetter = new Regex(@"[A-Za-z]+");
        var hasRepeated = new Regex(@"(.)\1\1");
        var hasSpecial = new Regex(@"[^\w]+");
        return hasNumber.IsMatch(password) && hasLetter.IsMatch(password)
               && !hasRepeated.IsMatch(password) && hasSpecial.IsMatch(password);
    }

    private string CreateToken(long userId, long idEmpresa, string usuario, string canal, DateTime expires)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtSigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim("IDEmpresa", idEmpresa.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, usuario),
            new Claim("canal", canal)
        };

        var jwt = new JwtSecurityToken(
            issuer: _options.JwtIssuer,
            audience: _options.JwtAudience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string Md5Hex(string input)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>MD5 ASCII — misma función que AuthenticationService.GetCalculateMd5Hash del ERP.</summary>
    public static string Md5HexAscii(string input)
    {
        var bytes = MD5.HashData(Encoding.ASCII.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static bool IsStorefrontSha256(string stored)
        => stored.Length == 64 && stored.All(char.IsAsciiHexDigit);

    public static bool VerifyPassword(string password, string stored)
    {
        if (string.IsNullOrEmpty(stored) || string.Equals(stored, "disabled", StringComparison.OrdinalIgnoreCase))
            return false;

        if (stored.StartsWith("plain:", StringComparison.Ordinal))
            return stored["plain:".Length..] == password;

        if (string.Equals(HashPassword(password), stored, StringComparison.OrdinalIgnoreCase))
            return true;

        // Cuentas espejo ERP: MD5 plano o BCrypt(MD5(password)).
        var md5 = Md5HexAscii(password);
        if (string.Equals(md5, stored, StringComparison.OrdinalIgnoreCase))
            return true;

        if (stored.StartsWith("$2", StringComparison.Ordinal))
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(md5, stored)
                       || BCrypt.Net.BCrypt.Verify(password, stored);
            }
            catch (Exception)
            {
                return false;
            }
        }

        return false;
    }
}
