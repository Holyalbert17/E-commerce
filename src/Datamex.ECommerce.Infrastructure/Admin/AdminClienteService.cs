using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Application.Catalog;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Datamex.ECommerce.Infrastructure.Pricing;
using Microsoft.EntityFrameworkCore;

namespace Datamex.ECommerce.Infrastructure.Admin;

public sealed class AdminClienteService : IAdminClienteService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAdminListaPrecioService _listas;

    public AdminClienteService(EcomDbContext db, ITenantContext tenant, IAdminListaPrecioService listas)
    {
        _db = db;
        _tenant = tenant;
        _listas = listas;
    }

    private long EmpresaId => _tenant.IDEmpresa
        ?? throw new InvalidOperationException("Tenant no resuelto.");

    public async Task<PagedResult<AdminClienteDto>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.Cuentas.AsNoTracking().Where(c => c.IDEmpresa == idEmpresa);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            var cuentaIdsByDoc = await (
                from link in _db.UsuariosEcommerceCuenta.AsNoTracking()
                join u in _db.UsuariosEcommerce.AsNoTracking() on link.IDUsuarioEcommerce equals u.ID
                where link.IDEmpresa == idEmpresa && link.Activo
                      && u.NumeroDocumento != null
                      && u.NumeroDocumento.ToLower().Contains(term)
                select link.IDCuenta
            ).Distinct().ToListAsync(ct);

            q = q.Where(c =>
                c.RazonSocial.ToLower().Contains(term)
                || (c.Email != null && c.Email.ToLower().Contains(term))
                || (c.NombreCuenta != null && c.NombreCuenta.ToLower().Contains(term))
                || c.NroCuenta.ToString().Contains(term)
                || cuentaIdsByDoc.Contains(c.ID));
        }

        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderBy(c => c.NroCuenta)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var listaIds = rows.Where(r => r.IDListaPrecio.HasValue).Select(r => r.IDListaPrecio!.Value).Distinct().ToList();
        var listas = await _db.ListasPrecio.AsNoTracking()
            .Where(l => l.IDEmpresa == idEmpresa && listaIds.Contains(l.ID))
            .ToDictionaryAsync(l => l.ID, l => l.Descripcion, ct);

        var contacts = await LoadContactsAsync(rows.Select(r => r.ID).ToList(), idEmpresa, ct);
        var items = rows.Select(c => Map(
            c,
            c.IDListaPrecio is long id && listas.TryGetValue(id, out var d) ? d : null,
            contacts.GetValueOrDefault(c.ID))).ToList();
        return new PagedResult<AdminClienteDto>(items, page, pageSize, total);
    }

    public async Task<AdminClienteDto?> GetAsync(long id, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var c = await _db.Cuentas.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct);
        if (c is null) return null;

        string? listaDesc = null;
        if (c.IDListaPrecio is long lid)
        {
            listaDesc = await _db.ListasPrecio.AsNoTracking()
                .Where(l => l.ID == lid && l.IDEmpresa == idEmpresa)
                .Select(l => l.Descripcion)
                .FirstOrDefaultAsync(ct);
        }

        var contacts = await LoadContactsAsync(new[] { c.ID }, idEmpresa, ct);
        return Map(c, listaDesc, contacts.GetValueOrDefault(c.ID));
    }

    public async Task<AdminClienteDto> CreateAsync(UpsertClienteRequest request, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        Validate(request);
        if (request.IDListaPrecio is long lid)
            await EnsureListaEcomAsync(idEmpresa, lid, ct);

        var now = DateTime.UtcNow;
        var nro = ((await _db.Cuentas.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa)
            .MaxAsync(c => (long?)c.NroCuenta, ct)) ?? 0) + 1;

        var c = new Cuenta
        {
            IDEmpresa = idEmpresa,
            Activo = request.Activo,
            UltModificacion = now,
            UserName = "admin",
            NroCuenta = nro,
            RazonSocial = request.RazonSocial.Trim(),
            NombreCuenta = NullIfEmpty(request.NombreCuenta),
            ApellidoCuenta = NullIfEmpty(request.ApellidoCuenta),
            Email = NullIfEmpty(request.Email),
            Direccion = NullIfEmpty(request.Direccion),
            IDListaPrecio = request.IDListaPrecio is > 0 ? request.IDListaPrecio : null,
            IDAgencia = request.IDAgencia is > 0 ? request.IDAgencia : null
        };
        _db.Cuentas.Add(c);
        await _db.SaveChangesAsync(ct);
        if (c.IDListaPrecio is long lidCreate and > 0)
            await ListaPrecioCuentaHelper.EnsureAssignmentAsync(_db, idEmpresa, c.ID, lidCreate, "admin", ct);

        await UpsertPrincipalContactAsync(c.ID, request, ct);
        return (await GetAsync(c.ID, ct))!;
    }

    public async Task<AdminClienteDto> UpdateAsync(long id, UpsertClienteRequest request, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        Validate(request);
        var c = await _db.Cuentas.FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Cliente no encontrado.");

        if (request.IDListaPrecio is long lid and > 0)
            await EnsureListaEcomAsync(idEmpresa, lid, ct);

        c.RazonSocial = request.RazonSocial.Trim();
        c.NombreCuenta = NullIfEmpty(request.NombreCuenta);
        c.ApellidoCuenta = NullIfEmpty(request.ApellidoCuenta);
        c.Email = NullIfEmpty(request.Email);
        c.Direccion = NullIfEmpty(request.Direccion);
        c.IDAgencia = request.IDAgencia is > 0 ? request.IDAgencia : null;
        c.Activo = request.Activo;
        c.UltModificacion = DateTime.UtcNow;
        c.UserName = "admin";

        if (request.IDListaPrecio is > 0)
        {
            await ListaPrecioCuentaHelper.EnsureAssignmentAsync(
                _db, idEmpresa, c.ID, request.IDListaPrecio.Value, "admin", ct);
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            c.IDListaPrecio = null;
            var vigentes = await _db.ListasPrecioCuenta
                .Where(x => x.IDEmpresa == idEmpresa && x.IDCuenta == c.ID && x.Activo)
                .ToListAsync(ct);
            foreach (var row in vigentes)
            {
                row.Activo = false;
                row.UltModificacion = DateTime.UtcNow;
                row.UserName = "admin";
            }
            await _db.SaveChangesAsync(ct);
        }

        await UpsertPrincipalContactAsync(c.ID, request, ct);
        return (await GetAsync(id, ct))!;
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var c = await _db.Cuentas.FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Cliente no encontrado.");
        c.Activo = false;
        c.UltModificacion = DateTime.UtcNow;
        c.UserName = "admin";
        await _db.SaveChangesAsync(ct);
    }

    public async Task<AdminClienteDto> AsignarListaPrecioAsync(
        long id, AsignarListaPrecioRequest request, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        if (request.IDListaPrecio <= 0)
            throw new ArgumentException("IDListaPrecio requerido.");

        await EnsureListaEcomAsync(idEmpresa, request.IDListaPrecio, ct);

        var c = await _db.Cuentas.FirstOrDefaultAsync(x => x.ID == id && x.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Cliente no encontrado.");

        await ListaPrecioCuentaHelper.EnsureAssignmentAsync(
            _db, idEmpresa, c.ID, request.IDListaPrecio, "admin", ct);
        return (await GetAsync(id, ct))!;
    }

    public async Task<IReadOnlyList<AdminListaPrecioCuentaDto>> HistorialListasAsync(
        long idCuenta, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var exists = await _db.Cuentas.AsNoTracking()
            .AnyAsync(c => c.ID == idCuenta && c.IDEmpresa == idEmpresa, ct);
        if (!exists)
            throw new InvalidOperationException("Cliente no encontrado.");

        var rows = await (
            from lpc in _db.ListasPrecioCuenta.AsNoTracking()
            join lp in _db.ListasPrecio.AsNoTracking() on lpc.IDListaPrecio equals lp.ID into lj
            from lp in lj.DefaultIfEmpty()
            where lpc.IDEmpresa == idEmpresa && lpc.IDCuenta == idCuenta
            orderby lpc.UltModificacion descending, lpc.ID descending
            select new AdminListaPrecioCuentaDto(
                lpc.ID,
                lpc.IDCuenta,
                lpc.IDListaPrecio,
                lp != null ? lp.Descripcion : null,
                lp != null ? lp.NroListaPrecio : null,
                lpc.Activo,
                lpc.UltModificacion,
                lpc.UserName)
        ).ToListAsync(ct);

        return rows;
    }

    public async Task<IReadOnlyList<AdminListaPrecioDto>> ListasVigentesAsync(CancellationToken ct = default)
    {
        var all = await _listas.ListAsync(ct);
        var now = DateTime.UtcNow;
        return all
            .Where(l => l.Activo && l.EsListaPrecioEcommerce && l.Estado == 2
                        && (l.FechaInicioVigencia is null || l.FechaInicioVigencia <= now)
                        && (l.FechaFinVigencia is null || l.FechaFinVigencia >= now))
            .ToList();
    }

    public async Task<IReadOnlyList<AdminMayoristaSolicitudDto>> ListSolicitudesMayoristaAsync(
        CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var users = await _db.UsuariosEcommerce.AsNoTracking()
            .Where(u => u.IDEmpresa == idEmpresa
                        && (u.EstadoMayorista == "Solicitado" || u.EstadoMayorista == "Aprobado"))
            .OrderByDescending(u => u.FechaSolicitudMayorista)
            .Take(100)
            .ToListAsync(ct);

        var result = new List<AdminMayoristaSolicitudDto>();
        foreach (var u in users)
        {
            var link = await _db.UsuariosEcommerceCuenta.AsNoTracking()
                .Where(x => x.IDUsuarioEcommerce == u.ID && x.IDEmpresa == idEmpresa && x.Activo)
                .OrderByDescending(x => x.EsPrincipal)
                .FirstOrDefaultAsync(ct);
            string? razon = null;
            long? idLista = null;
            if (link != null)
            {
                var c = await _db.Cuentas.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ID == link.IDCuenta, ct);
                razon = c?.RazonSocial;
                idLista = c?.IDListaPrecio;
            }

            result.Add(new AdminMayoristaSolicitudDto(
                u.ID,
                $"{u.Nombre} {u.Apellido}".Trim(),
                u.Email,
                u.EstadoMayorista,
                u.FechaSolicitudMayorista,
                u.MotivoSolicitudMayorista,
                u.MontoComprasAcumulado,
                link?.IDCuenta,
                razon,
                idLista));
        }

        return result;
    }

    private async Task EnsureListaEcomAsync(long idEmpresa, long idLista, CancellationToken ct)
    {
        // Estado 2 = AUTORIZADO (vigente para asignación a clientes)
        var ok = await _db.ListasPrecio.AnyAsync(l =>
            l.ID == idLista && l.IDEmpresa == idEmpresa && l.Activo
            && l.EsListaPrecioEcommerce && l.Estado == 2, ct);
        if (!ok)
            throw new InvalidOperationException("Lista de precio e-commerce no válida o no autorizada.");
    }

    private static void Validate(UpsertClienteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RazonSocial))
            throw new ArgumentException("RazonSocial requerida.");
    }

    private static string? NullIfEmpty(string? v) =>
        string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    private sealed record ContactInfo(long? IdUsuario, long? IdTipoDocumento, string? NumeroDocumento, string? Telefono);

    private async Task<Dictionary<long, ContactInfo>> LoadContactsAsync(
        IReadOnlyList<long> cuentaIds, long idEmpresa, CancellationToken ct)
    {
        var result = new Dictionary<long, ContactInfo>();
        if (cuentaIds.Count == 0) return result;

        var links = await _db.UsuariosEcommerceCuenta.AsNoTracking()
            .Where(x => x.IDEmpresa == idEmpresa && x.Activo && cuentaIds.Contains(x.IDCuenta))
            .OrderByDescending(x => x.EsPrincipal)
            .ThenBy(x => x.ID)
            .ToListAsync(ct);

        var picked = new Dictionary<long, long>();
        foreach (var link in links)
        {
            if (!picked.ContainsKey(link.IDCuenta))
                picked[link.IDCuenta] = link.IDUsuarioEcommerce;
        }

        if (picked.Count == 0) return result;

        var userIds = picked.Values.Distinct().ToList();
        var users = await _db.UsuariosEcommerce.AsNoTracking()
            .Where(u => u.IDEmpresa == idEmpresa && userIds.Contains(u.ID))
            .ToDictionaryAsync(u => u.ID, ct);

        foreach (var (cuentaId, userId) in picked)
        {
            if (!users.TryGetValue(userId, out var u)) continue;
            var tel = NullIfEmpty(u.NumeroMovil) ?? NullIfEmpty(u.NumeroTelefono);
            result[cuentaId] = new ContactInfo(u.ID, u.IDTipoDocumento, u.NumeroDocumento, tel);
        }

        return result;
    }

    private async Task UpsertPrincipalContactAsync(long idCuenta, UpsertClienteRequest request, CancellationToken ct)
    {
        var idEmpresa = EmpresaId;
        var hasContact =
            request.IDTipoDocumento is > 0
            || !string.IsNullOrWhiteSpace(request.NumeroDocumento)
            || !string.IsNullOrWhiteSpace(request.Telefono);

        var link = await _db.UsuariosEcommerceCuenta
            .Where(x => x.IDEmpresa == idEmpresa && x.IDCuenta == idCuenta && x.Activo)
            .OrderByDescending(x => x.EsPrincipal)
            .ThenBy(x => x.ID)
            .FirstOrDefaultAsync(ct);

        UsuarioEcommerce? user = null;
        if (link != null)
        {
            user = await _db.UsuariosEcommerce
                .FirstOrDefaultAsync(u => u.ID == link.IDUsuarioEcommerce && u.IDEmpresa == idEmpresa, ct);
        }
        else if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim().ToLowerInvariant();
            user = await _db.UsuariosEcommerce
                .FirstOrDefaultAsync(u => u.IDEmpresa == idEmpresa && u.Email != null && u.Email.ToLower() == email, ct);
            if (user != null)
            {
                _db.UsuariosEcommerceCuenta.Add(new UsuarioEcommerceCuenta
                {
                    IDEmpresa = idEmpresa,
                    IDUsuarioEcommerce = user.ID,
                    IDCuenta = idCuenta,
                    EsPrincipal = true,
                    Activo = true,
                    UltModificacion = DateTime.UtcNow,
                    UserName = "admin"
                });
                await _db.SaveChangesAsync(ct);
            }
        }

        if (user is null)
        {
            if (!hasContact) return;
            var now = DateTime.UtcNow;
            var login = NullIfEmpty(request.Email) ?? $"cuenta{idCuenta}@local.admin";
            user = new UsuarioEcommerce
            {
                IDEmpresa = idEmpresa,
                Activo = true,
                Activado = false,
                Bloqueado = true,
                UltModificacion = now,
                UserName = "admin",
                CodigoUsuario = $"ADM-{idCuenta}",
                Usuario = login,
                Password = "disabled",
                Nombre = NullIfEmpty(request.NombreCuenta) ?? request.RazonSocial.Trim(),
                Apellido = NullIfEmpty(request.ApellidoCuenta) ?? "",
                Email = NullIfEmpty(request.Email),
                IDTipoDocumento = request.IDTipoDocumento is > 0 ? request.IDTipoDocumento : null,
                NumeroDocumento = NullIfEmpty(request.NumeroDocumento),
                NumeroTelefono = NullIfEmpty(request.Telefono),
                NumeroMovil = NullIfEmpty(request.Telefono),
                FechaRegistro = now,
                CanalPreferido = "B2C"
            };
            _db.UsuariosEcommerce.Add(user);
            await _db.SaveChangesAsync(ct);
            _db.UsuariosEcommerceCuenta.Add(new UsuarioEcommerceCuenta
            {
                IDEmpresa = idEmpresa,
                IDUsuarioEcommerce = user.ID,
                IDCuenta = idCuenta,
                EsPrincipal = true,
                Activo = true,
                UltModificacion = now,
                UserName = "admin"
            });
            await _db.SaveChangesAsync(ct);
            return;
        }

        user.IDTipoDocumento = request.IDTipoDocumento is > 0 ? request.IDTipoDocumento : null;
        user.NumeroDocumento = NullIfEmpty(request.NumeroDocumento);
        var tel = NullIfEmpty(request.Telefono);
        user.NumeroTelefono = tel;
        user.NumeroMovil = tel;
        if (NullIfEmpty(request.Email) is string em)
            user.Email = em;
        user.UltModificacion = DateTime.UtcNow;
        user.UserName = "admin";
        await _db.SaveChangesAsync(ct);
    }

    private static AdminClienteDto Map(Cuenta c, string? listaDesc, ContactInfo? contact = null) => new(
        c.ID, c.NroCuenta, c.RazonSocial, c.NombreCuenta, c.ApellidoCuenta,
        c.Email, c.Direccion, c.IDListaPrecio, listaDesc, c.IDAgencia,
        c.Activo, c.UltModificacion,
        contact?.IdTipoDocumento, contact?.NumeroDocumento, contact?.Telefono, contact?.IdUsuario);
}
