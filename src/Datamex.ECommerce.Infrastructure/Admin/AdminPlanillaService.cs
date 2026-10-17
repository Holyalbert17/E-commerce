using System.Globalization;
using System.Text;
using Datamex.ECommerce.Application.Abstractions;
using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace Datamex.ECommerce.Infrastructure.Admin;

public sealed class AdminPlanillaService : IAdminPlanillaService
{
    private readonly EcomDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IAdminCatalogService _catalog;

    public AdminPlanillaService(EcomDbContext db, ITenantContext tenant, IAdminCatalogService catalog)
    {
        _db = db;
        _tenant = tenant;
        _catalog = catalog;
    }

    private long EmpresaId => _tenant.IDEmpresa
        ?? throw new InvalidOperationException("Tenant no resuelto.");

    public async Task<byte[]> ExportClientesCsvAsync(CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var rows = await _db.Cuentas.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa)
            .OrderBy(c => c.NroCuenta)
            .Select(c => new
            {
                c.ID,
                c.NroCuenta,
                c.RazonSocial,
                c.Email,
                c.IDListaPrecio,
                c.Activo
            })
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Id,NroCuenta,RazonSocial,Email,IDListaPrecio,Activo");
        foreach (var r in rows)
        {
            sb.Append(r.ID).Append(',')
                .Append(r.NroCuenta).Append(',')
                .Append(Csv(r.RazonSocial)).Append(',')
                .Append(Csv(r.Email ?? "")).Append(',')
                .Append(r.IDListaPrecio?.ToString() ?? "").Append(',')
                .Append(r.Activo ? "1" : "0")
                .AppendLine();
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportClientesExcelAsync(CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var rows = await _db.Cuentas.AsNoTracking()
            .Where(c => c.IDEmpresa == idEmpresa)
            .OrderBy(c => c.NroCuenta)
            .Select(c => new { c.ID, c.NroCuenta, c.RazonSocial, c.Email, c.IDListaPrecio, c.Activo })
            .ToListAsync(ct);

        using var pkg = new ExcelPackage();
        var ws = pkg.Workbook.Worksheets.Add("Clientes");
        ws.Cells[1, 1].Value = "Id";
        ws.Cells[1, 2].Value = "NroCuenta";
        ws.Cells[1, 3].Value = "RazonSocial";
        ws.Cells[1, 4].Value = "Email";
        ws.Cells[1, 5].Value = "IDListaPrecio";
        ws.Cells[1, 6].Value = "Activo";
        var r = 2;
        foreach (var row in rows)
        {
            ws.Cells[r, 1].Value = row.ID;
            ws.Cells[r, 2].Value = row.NroCuenta;
            ws.Cells[r, 3].Value = row.RazonSocial;
            ws.Cells[r, 4].Value = row.Email;
            ws.Cells[r, 5].Value = row.IDListaPrecio;
            ws.Cells[r, 6].Value = row.Activo ? 1 : 0;
            r++;
        }
        ws.Cells[1, 1, 1, 6].Style.Font.Bold = true;
        ws.Cells.AutoFitColumns();
        return pkg.GetAsByteArray();
    }

    public async Task<byte[]> ExportListaPreciosCsvAsync(long listaId, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var lista = await _db.ListasPrecio.AsNoTracking()
            .FirstOrDefaultAsync(l => l.ID == listaId && l.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Lista de precio no encontrada.");

        var rows = await (
            from d in _db.DetallesListaPrecioCondicionVenta.AsNoTracking()
            join e in _db.ArticuloEmbalajes.AsNoTracking() on d.IDArticuloEmbalaje equals e.ID
            join a in _db.Articulos.AsNoTracking() on e.IDArticulo equals a.ID
            where d.IDEmpresa == idEmpresa && d.IDListaPrecio == listaId
            orderby a.SKU
            select new
            {
                d.ID,
                a.SKU,
                a.DescripcionArticulo,
                d.IDArticuloEmbalaje,
                d.PrecioContado,
                d.PrecioCheque,
                d.PrecioCredito,
                d.Activo
            }).ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Id,SKU,DescripcionArticulo,IDArticuloEmbalaje,PrecioContado,PrecioCheque,PrecioCredito,Activo,ListaDescripcion");
        foreach (var r in rows)
        {
            sb.Append(r.ID).Append(',')
                .Append(Csv(r.SKU ?? "")).Append(',')
                .Append(Csv(r.DescripcionArticulo)).Append(',')
                .Append(r.IDArticuloEmbalaje).Append(',')
                .Append(Fmt(r.PrecioContado)).Append(',')
                .Append(Fmt(r.PrecioCheque)).Append(',')
                .Append(Fmt(r.PrecioCredito)).Append(',')
                .Append(r.Activo ? "1" : "0").Append(',')
                .Append(Csv(lista.Descripcion))
                .AppendLine();
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportListaPreciosExcelAsync(long listaId, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var lista = await _db.ListasPrecio.AsNoTracking()
            .FirstOrDefaultAsync(l => l.ID == listaId && l.IDEmpresa == idEmpresa, ct)
            ?? throw new InvalidOperationException("Lista de precio no encontrada.");

        var rows = await (
            from d in _db.DetallesListaPrecioCondicionVenta.AsNoTracking()
            join e in _db.ArticuloEmbalajes.AsNoTracking() on d.IDArticuloEmbalaje equals e.ID
            join a in _db.Articulos.AsNoTracking() on e.IDArticulo equals a.ID
            where d.IDEmpresa == idEmpresa && d.IDListaPrecio == listaId
            orderby a.SKU
            select new
            {
                d.ID,
                a.SKU,
                a.DescripcionArticulo,
                d.IDArticuloEmbalaje,
                d.PrecioContado,
                d.PrecioCheque,
                d.PrecioCredito,
                d.Activo
            }).ToListAsync(ct);

        using var pkg = new ExcelPackage();
        var ws = pkg.Workbook.Worksheets.Add("ListaPrecios");
        string[] headers =
        [
            "Id", "SKU", "DescripcionArticulo", "IDArticuloEmbalaje",
            "PrecioContado", "PrecioCheque", "PrecioCredito", "Activo", "ListaDescripcion"
        ];
        for (var c = 0; c < headers.Length; c++)
            ws.Cells[1, c + 1].Value = headers[c];
        var r = 2;
        foreach (var row in rows)
        {
            ws.Cells[r, 1].Value = row.ID;
            ws.Cells[r, 2].Value = row.SKU;
            ws.Cells[r, 3].Value = row.DescripcionArticulo;
            ws.Cells[r, 4].Value = row.IDArticuloEmbalaje;
            ws.Cells[r, 5].Value = (double)row.PrecioContado;
            ws.Cells[r, 6].Value = (double)row.PrecioCheque;
            ws.Cells[r, 7].Value = (double)row.PrecioCredito;
            ws.Cells[r, 8].Value = row.Activo ? 1 : 0;
            ws.Cells[r, 9].Value = lista.Descripcion;
            r++;
        }
        ws.Cells[1, 1, 1, headers.Length].Style.Font.Bold = true;
        ws.Cells.AutoFitColumns();
        return pkg.GetAsByteArray();
    }

    public async Task<byte[]> ExportArticulosCsvAsync(CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var rows = await _db.Articulos.AsNoTracking()
            .Where(a => a.IDEmpresa == idEmpresa)
            .OrderBy(a => a.NroArticulo)
            .Select(a => new
            {
                a.ID,
                a.SKU,
                a.DescripcionArticulo,
                a.NombreMarca,
                a.ActivoEcommerce,
                a.EstadoEcommerce,
                a.Activo
            })
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Id,SKU,DescripcionArticulo,NombreMarca,ActivoEcommerce,EstadoEcommerce,Activo");
        foreach (var r in rows)
        {
            sb.Append(r.ID).Append(',')
                .Append(Csv(r.SKU ?? "")).Append(',')
                .Append(Csv(r.DescripcionArticulo)).Append(',')
                .Append(Csv(r.NombreMarca ?? "")).Append(',')
                .Append(r.ActivoEcommerce ? "1" : "0").Append(',')
                .Append(Csv(r.EstadoEcommerce)).Append(',')
                .Append(r.Activo ? "1" : "0")
                .AppendLine();
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportArticulosExcelAsync(CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var rows = await _db.Articulos.AsNoTracking()
            .Where(a => a.IDEmpresa == idEmpresa)
            .OrderBy(a => a.NroArticulo)
            .Select(a => new
            {
                a.ID,
                a.SKU,
                a.DescripcionArticulo,
                a.NombreMarca,
                a.ActivoEcommerce,
                a.EstadoEcommerce,
                a.Activo
            })
            .ToListAsync(ct);

        using var pkg = new ExcelPackage();
        var ws = pkg.Workbook.Worksheets.Add("Articulos");
        string[] headers =
            ["Id", "SKU", "DescripcionArticulo", "NombreMarca", "ActivoEcommerce", "EstadoEcommerce", "Activo"];
        for (var c = 0; c < headers.Length; c++)
            ws.Cells[1, c + 1].Value = headers[c];
        var r = 2;
        foreach (var row in rows)
        {
            ws.Cells[r, 1].Value = row.ID;
            ws.Cells[r, 2].Value = row.SKU;
            ws.Cells[r, 3].Value = row.DescripcionArticulo;
            ws.Cells[r, 4].Value = row.NombreMarca;
            ws.Cells[r, 5].Value = row.ActivoEcommerce ? 1 : 0;
            ws.Cells[r, 6].Value = row.EstadoEcommerce;
            ws.Cells[r, 7].Value = row.Activo ? 1 : 0;
            r++;
        }
        ws.Cells[1, 1, 1, headers.Length].Style.Font.Bold = true;
        ws.Cells.AutoFitColumns();
        return pkg.GetAsByteArray();
    }

    public async Task<PlanillaImportResult> ImportClientesCsvAsync(Stream csv, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var lines = await ReadCsvLinesAsync(csv, ct);
        if (lines.Count < 2)
            return new PlanillaImportResult(0, 0, 0, ["CSV vacío o sin datos."]);

        var header = ParseCsvLine(lines[0]);
        var idxId = Col(header, "Id");
        var idxNro = Col(header, "NroCuenta");
        var idxRazon = Col(header, "RazonSocial");
        var idxEmail = Col(header, "Email");
        var idxLista = Col(header, "IDListaPrecio");
        var idxActivo = Col(header, "Activo");

        if (idxRazon < 0)
            return new PlanillaImportResult(0, 0, 0, ["Columna RazonSocial requerida."]);

        var inserted = 0;
        var updated = 0;
        var skipped = 0;
        var errors = new List<string>();
        var now = DateTime.UtcNow;

        for (var i = 1; i < lines.Count; i++)
        {
            var cols = ParseCsvLine(lines[i]);
            if (cols.Count == 0 || cols.All(string.IsNullOrWhiteSpace))
            {
                skipped++;
                continue;
            }

            try
            {
                var razon = Get(cols, idxRazon);
                if (string.IsNullOrWhiteSpace(razon))
                {
                    skipped++;
                    continue;
                }

                Cuenta? cuenta = null;
                if (idxId >= 0 && long.TryParse(Get(cols, idxId), out var id) && id > 0)
                {
                    cuenta = await _db.Cuentas
                        .FirstOrDefaultAsync(c => c.ID == id && c.IDEmpresa == idEmpresa, ct);
                }

                if (cuenta is null)
                {
                    var nro = idxNro >= 0 && long.TryParse(Get(cols, idxNro), out var n) && n > 0
                        ? n
                        : ((await _db.Cuentas.Where(c => c.IDEmpresa == idEmpresa)
                            .MaxAsync(c => (long?)c.NroCuenta, ct)) ?? 0) + 1;

                    cuenta = new Cuenta
                    {
                        IDEmpresa = idEmpresa,
                        NroCuenta = nro,
                        Activo = ParseBool(Get(cols, idxActivo), true),
                        UltModificacion = now,
                        UserName = "planilla-import"
                    };
                    _db.Cuentas.Add(cuenta);
                    inserted++;
                }
                else
                {
                    updated++;
                }

                cuenta.RazonSocial = razon.Trim();
                cuenta.Email = NullIfEmpty(Get(cols, idxEmail));
                if (idxLista >= 0 && long.TryParse(Get(cols, idxLista), out var lid) && lid > 0)
                    cuenta.IDListaPrecio = lid;
                if (idxActivo >= 0)
                    cuenta.Activo = ParseBool(Get(cols, idxActivo), cuenta.Activo);
                cuenta.UltModificacion = now;
                cuenta.UserName = "planilla-import";
            }
            catch (Exception ex)
            {
                errors.Add($"Fila {i + 1}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync(ct);
        return new PlanillaImportResult(inserted, updated, skipped, errors);
    }

    public async Task<PlanillaImportResult> ImportListaPreciosCsvAsync(
        long listaId, Stream csv, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var listaExists = await _db.ListasPrecio.AsNoTracking()
            .AnyAsync(l => l.ID == listaId && l.IDEmpresa == idEmpresa, ct);
        if (!listaExists)
            throw new InvalidOperationException("Lista de precio no encontrada.");

        var lines = await ReadCsvLinesAsync(csv, ct);
        if (lines.Count < 2)
            return new PlanillaImportResult(0, 0, 0, ["CSV vacío o sin datos."]);

        var header = ParseCsvLine(lines[0]);
        var idxEmb = Col(header, "IDArticuloEmbalaje");
        var idxContado = Col(header, "PrecioContado");
        var idxCheque = Col(header, "PrecioCheque");
        var idxCredito = Col(header, "PrecioCredito");
        var idxActivo = Col(header, "Activo");

        if (idxEmb < 0)
            return new PlanillaImportResult(0, 0, 0, ["Columna IDArticuloEmbalaje requerida."]);

        var inserted = 0;
        var updated = 0;
        var skipped = 0;
        var errors = new List<string>();
        var now = DateTime.UtcNow;

        for (var i = 1; i < lines.Count; i++)
        {
            var cols = ParseCsvLine(lines[i]);
            if (cols.Count == 0 || cols.All(string.IsNullOrWhiteSpace))
            {
                skipped++;
                continue;
            }

            try
            {
                if (!long.TryParse(Get(cols, idxEmb), out var embId) || embId <= 0)
                {
                    skipped++;
                    continue;
                }

                var embExists = await _db.ArticuloEmbalajes.AsNoTracking()
                    .AnyAsync(e => e.ID == embId && e.IDEmpresa == idEmpresa, ct);
                if (!embExists)
                {
                    errors.Add($"Fila {i + 1}: embalaje {embId} no encontrado.");
                    continue;
                }

                var det = await _db.DetallesListaPrecioCondicionVenta
                    .FirstOrDefaultAsync(d => d.IDEmpresa == idEmpresa
                                              && d.IDListaPrecio == listaId
                                              && d.IDArticuloEmbalaje == embId, ct);

                if (det is null)
                {
                    det = new DetalleListaPrecioCondicionVenta
                    {
                        IDEmpresa = idEmpresa,
                        IDListaPrecio = listaId,
                        IDArticuloEmbalaje = embId,
                        Activo = true,
                        UltModificacion = now,
                        UserName = "planilla-import"
                    };
                    _db.DetallesListaPrecioCondicionVenta.Add(det);
                    inserted++;
                }
                else
                {
                    updated++;
                }

                if (idxContado >= 0 && decimal.TryParse(Get(cols, idxContado), NumberStyles.Any, CultureInfo.InvariantCulture, out var pc))
                    det.PrecioContado = pc;
                if (idxCheque >= 0 && decimal.TryParse(Get(cols, idxCheque), NumberStyles.Any, CultureInfo.InvariantCulture, out var pch))
                    det.PrecioCheque = pch;
                if (idxCredito >= 0 && decimal.TryParse(Get(cols, idxCredito), NumberStyles.Any, CultureInfo.InvariantCulture, out var pcr))
                    det.PrecioCredito = pcr;
                if (idxActivo >= 0)
                    det.Activo = ParseBool(Get(cols, idxActivo), det.Activo);
                det.UltModificacion = now;
                det.UserName = "planilla-import";
            }
            catch (Exception ex)
            {
                errors.Add($"Fila {i + 1}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync(ct);
        return new PlanillaImportResult(inserted, updated, skipped, errors);
    }

    public async Task<PlanillaImportResult> ImportArticulosActivoCsvAsync(Stream csv, CancellationToken ct = default)
    {
        var idEmpresa = EmpresaId;
        var lines = await ReadCsvLinesAsync(csv, ct);
        if (lines.Count < 2)
            return new PlanillaImportResult(0, 0, 0, ["CSV vacío o sin datos."]);

        var header = ParseCsvLine(lines[0]);
        var idxId = Col(header, "Id");
        var idxSku = Col(header, "SKU");
        var idxDesc = Col(header, "DescripcionArticulo");
        var idxMarca = Col(header, "NombreMarca");
        var idxActivoEcom = Col(header, "ActivoEcommerce");
        var idxEstado = Col(header, "EstadoEcommerce");
        var idxRef = Col(header, "CodReferenciaProveedor");
        var idxBarra = Col(header, "CodigoBarra");
        var idxCant = Col(header, "CantidadPorEmbalaje");

        if (idxActivoEcom < 0 && idxEstado < 0 && idxDesc < 0)
            return new PlanillaImportResult(0, 0, 0,
                ["Se requiere ActivoEcommerce/EstadoEcommerce (update) o DescripcionArticulo (alta)."]);

        var defaults = await ResolveDefaultParamsAsync(idEmpresa, ct);

        var inserted = 0;
        var updated = 0;
        var skipped = 0;
        var errors = new List<string>();
        var now = DateTime.UtcNow;

        for (var i = 1; i < lines.Count; i++)
        {
            var cols = ParseCsvLine(lines[i]);
            if (cols.Count == 0 || cols.All(string.IsNullOrWhiteSpace))
            {
                skipped++;
                continue;
            }

            try
            {
                Articulo? art = null;
                if (idxId >= 0 && long.TryParse(Get(cols, idxId), out var id) && id > 0)
                {
                    art = await _db.Articulos
                        .FirstOrDefaultAsync(a => a.ID == id && a.IDEmpresa == idEmpresa, ct);
                }
                else if (idxSku >= 0)
                {
                    var skuLookup = Get(cols, idxSku);
                    if (!string.IsNullOrWhiteSpace(skuLookup))
                    {
                        art = await _db.Articulos
                            .FirstOrDefaultAsync(a => a.IDEmpresa == idEmpresa && a.SKU == skuLookup, ct);
                    }
                }

                if (art is null)
                {
                    var sku = idxSku >= 0 ? Get(cols, idxSku) : null;
                    var desc = idxDesc >= 0 ? Get(cols, idxDesc) : null;
                    if (string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(desc) || defaults is null)
                    {
                        skipped++;
                        continue;
                    }

                    var d = defaults.Value;
                    var marcaId = d.IdMarca;
                    if (idxMarca >= 0)
                    {
                        var marcaNombre = Get(cols, idxMarca);
                        if (!string.IsNullOrWhiteSpace(marcaNombre))
                        {
                            var m = await _db.CatalogoParametros.AsNoTracking()
                                .FirstOrDefaultAsync(p =>
                                    p.IDEmpresa == idEmpresa
                                    && p.Tipo == CatalogoParametroTipos.Marca
                                    && p.Activo
                                    && p.Descripcion == marcaNombre.Trim(), ct);
                            if (m is not null) marcaId = m.ID;
                        }
                    }

                    double cant = 1;
                    if (idxCant >= 0 && double.TryParse(Get(cols, idxCant), NumberStyles.Any, CultureInfo.InvariantCulture, out var cParsed))
                        cant = Math.Max(1, cParsed);

                    await _catalog.CreateAsync(new CreateAdminArticuloRequest(
                        Sku: sku.Trim(),
                        Descripcion: desc.Trim(),
                        CodReferenciaProveedor: idxRef >= 0 ? Get(cols, idxRef) : null,
                        IdMarca: marcaId,
                        IdLinea: d.IdLinea,
                        IdTipoArticulo: d.IdTipo,
                        IdProveedor: d.IdProveedor,
                        IdEmbalaje: d.IdEmbalaje,
                        CodigoUnidadMedida: "UN",
                        CantidadPorEmbalaje: cant,
                        CodigoBarra: idxBarra >= 0 ? Get(cols, idxBarra) : null), ct);

                    // Re-fetch to apply vitrina flags if present
                    art = await _db.Articulos
                        .FirstOrDefaultAsync(a => a.IDEmpresa == idEmpresa && a.SKU == sku.Trim(), ct);
                    inserted++;
                    if (art is null)
                        continue;
                }
                else
                {
                    updated++;
                }

                if (idxActivoEcom >= 0)
                    art.ActivoEcommerce = ParseBool(Get(cols, idxActivoEcom), art.ActivoEcommerce);
                if (idxEstado >= 0)
                {
                    var est = Get(cols, idxEstado);
                    if (!string.IsNullOrWhiteSpace(est))
                        art.EstadoEcommerce = est.Trim();
                }

                art.UltModificacion = now;
                art.UserName = "planilla-import";
            }
            catch (Exception ex)
            {
                errors.Add($"Fila {i + 1}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync(ct);
        return new PlanillaImportResult(inserted, updated, skipped, errors);
    }

    private async Task<(long IdMarca, long IdLinea, long IdTipo, long IdProveedor, long IdEmbalaje)?> ResolveDefaultParamsAsync(
        long idEmpresa, CancellationToken ct)
    {
        async Task<long?> First(string tipo) =>
            await _db.CatalogoParametros.AsNoTracking()
                .Where(p => p.IDEmpresa == idEmpresa && p.Tipo == tipo && p.Activo)
                .OrderBy(p => p.Codigo)
                .Select(p => (long?)p.ID)
                .FirstOrDefaultAsync(ct);

        var marca = await First(CatalogoParametroTipos.Marca);
        var linea = await First(CatalogoParametroTipos.Linea);
        var tipo = await First(CatalogoParametroTipos.Tipo);
        var prov = await First(CatalogoParametroTipos.Proveedor);
        var emb = await First(CatalogoParametroTipos.Embalaje);
        if (marca is null || linea is null || tipo is null || prov is null || emb is null)
            return null;
        return (marca.Value, linea.Value, tipo.Value, prov.Value, emb.Value);
    }

    private static async Task<List<string>> ReadCsvLinesAsync(Stream csv, CancellationToken ct)
    {
        using var reader = new StreamReader(csv, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var lines = new List<string>();
        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct);
            if (line is not null)
                lines.Add(line);
        }
        return lines;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else inQuotes = false;
                }
                else sb.Append(c);
            }
            else if (c == '"') inQuotes = true;
            else if (c == ',')
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else sb.Append(c);
        }
        result.Add(sb.ToString());
        return result;
    }

    private static int Col(IReadOnlyList<string> header, string name)
    {
        for (var i = 0; i < header.Count; i++)
        {
            if (string.Equals(header[i].Trim(), name, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private static string Get(IReadOnlyList<string> cols, int idx) =>
        idx >= 0 && idx < cols.Count ? cols[idx].Trim() : "";

    private static bool ParseBool(string? v, bool fallback) =>
        v switch
        {
            "1" or "true" or "TRUE" or "True" or "si" or "SI" => true,
            "0" or "false" or "FALSE" or "False" or "no" or "NO" => false,
            _ when string.IsNullOrWhiteSpace(v) => fallback,
            _ => fallback
        };

    private static string? NullIfEmpty(string? v) =>
        string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    private static string Csv(string v) => "\"" + v.Replace("\"", "\"\"") + "\"";

    private static string Fmt(decimal d) => d.ToString(CultureInfo.InvariantCulture);
}
