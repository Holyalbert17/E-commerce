using Datamex.ECommerce.Application.Admin;
using Datamex.ECommerce.Domain.Entities;

namespace Datamex.ECommerce.Infrastructure.Admin;

/// <summary>
/// Árbol de operaciones estilo FancyTree ERP (<c>TreeEmpresaUsuarioOperacion</c>).
/// </summary>
public static class AdminPermissionTreeBuilder
{
    /// <summary>
    /// Origen del grant: P = directo (empresausuariooperacion), R = rol (empresaroloperacion).
    /// </summary>
    public const string SourceDirect = "P";
    public const string SourceRole = "R";

    public static IReadOnlyList<AdminPermissionTreeNodeDto> Build(
        IReadOnlyList<AdminFuncionalidad> catalog,
        IReadOnlySet<string> selectedDirect,
        IReadOnlySet<string> fromRole)
    {
        var byId = catalog.ToDictionary(f => f.ID);
        var children = catalog
            .GroupBy(f => f.IDFuncionalidadPadre)
            .ToDictionary(g => g.Key ?? 0L, g => g.OrderBy(x => x.Orden).ThenBy(x => x.CodigoPermission).ToList());

        // Raíces: sin padre, o padre inexistente (catálogo parcial).
        var roots = catalog
            .Where(f => f.IDFuncionalidadPadre is null or 0
                        || !byId.ContainsKey(f.IDFuncionalidadPadre.Value))
            .OrderBy(f => f.Orden)
            .ThenBy(f => f.CodigoPermission)
            .ToList();

        // Evitar duplicar nodos que también aparecen como hijos.
        var rootIds = roots.Select(r => r.ID).ToHashSet();
        foreach (var f in catalog)
        {
            if (f.IDFuncionalidadPadre is > 0 && byId.ContainsKey(f.IDFuncionalidadPadre.Value))
                rootIds.Remove(f.ID);
        }

        AdminPermissionTreeNodeDto Map(AdminFuncionalidad f)
        {
            var code = f.CodigoPermission;
            var fromR = fromRole.Contains(code);
            var fromP = selectedDirect.Contains(code) && !fromR;
            var kids = children.TryGetValue(f.ID, out var list)
                ? list.Select(Map).ToList()
                : new List<AdminPermissionTreeNodeDto>();

            return new AdminPermissionTreeNodeDto(
                Key: f.ID.ToString(),
                CodigoPermission: code,
                Title: f.Descripcion,
                Folder: kids.Count > 0,
                Selected: fromP || fromR,
                Unselectable: fromR,
                Source: fromR ? SourceRole : (fromP ? SourceDirect : null),
                Children: kids);
        }

        return catalog
            .Where(f => rootIds.Contains(f.ID))
            .OrderBy(f => f.Orden)
            .ThenBy(f => f.CodigoPermission)
            .Select(Map)
            .ToList();
    }

    /// <summary>
    /// Al guardar, igual ERP: incluir padres de cada hoja seleccionada.
    /// </summary>
    public static HashSet<long> ExpandWithAncestors(
        IEnumerable<long> selectedIds,
        IReadOnlyDictionary<long, AdminFuncionalidad> byId)
    {
        var result = new HashSet<long>();
        foreach (var id in selectedIds)
        {
            var cur = id;
            while (cur > 0 && result.Add(cur))
            {
                if (!byId.TryGetValue(cur, out var f) || f.IDFuncionalidadPadre is not > 0)
                    break;
                cur = f.IDFuncionalidadPadre.Value;
            }
        }
        return result;
    }

    /// <summary>
    /// Cablea <see cref="AdminFuncionalidad.IDFuncionalidadPadre"/> por prefijo de código
    /// (Site_App_Ecommerce_Productos_Add → Site_App_Ecommerce_Productos).
    /// </summary>
    public static void WireParentsByCodePrefix(IList<AdminFuncionalidad> rows)
    {
        var byCode = rows.ToDictionary(r => r.CodigoPermission, StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var code = row.CodigoPermission;
            string? bestParent = null;
            foreach (var candidate in byCode.Keys)
            {
                if (string.Equals(candidate, code, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!code.StartsWith(candidate + "_", StringComparison.OrdinalIgnoreCase)
                    && !code.StartsWith(candidate + ":", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (bestParent is null || candidate.Length > bestParent.Length)
                    bestParent = candidate;
            }

            if (bestParent is not null && byCode.TryGetValue(bestParent, out var parent))
            {
                row.IDFuncionalidadPadre = parent.ID;
            }
        }

        // Orden por profundidad / código
        var depth = new Dictionary<long, int>();
        int DepthOf(AdminFuncionalidad f)
        {
            if (depth.TryGetValue(f.ID, out var d)) return d;
            if (f.IDFuncionalidadPadre is not > 0) return depth[f.ID] = 0;
            var p = rows.FirstOrDefault(x => x.ID == f.IDFuncionalidadPadre);
            return depth[f.ID] = p is null ? 0 : DepthOf(p) + 1;
        }

        foreach (var row in rows)
            row.Orden = DepthOf(row) * 1000 + Array.IndexOf(
                rows.OrderBy(x => x.CodigoPermission).Select(x => x.ID).ToArray(), row.ID);
    }
}
