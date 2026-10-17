using Datamex.ECommerce.Domain.Enums;
using Datamex.ECommerce.Infrastructure;
using Datamex.ECommerce.Infrastructure.Persistence;
using Datamex.ECommerce.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

const long IdEmpresa = 41;
const string Slug = "casa-imperial";

var repoRoot = FindRepoRoot();
var apiPath = Path.Combine(repoRoot, "src", "Datamex.ECommerce.Api");
Directory.SetCurrentDirectory(apiPath);

var config = new ConfigurationBuilder()
    .SetBasePath(apiPath)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Configure Casa Imperial (empresa {IdEmpresa} / {Slug})");
Console.WriteLine($"  EcomDb: {Mask(config.GetConnectionString("EcomDb"))}");

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddSingleton<IConfiguration>(config);
services.AddECommerceInfrastructure(config);

await using var sp = services.BuildServiceProvider();
await using var scope = sp.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<EcomDbContext>();
var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("configure-ci");
var now = DateTime.UtcNow;

var empresa = await db.Empresas.FirstOrDefaultAsync(e => e.ID == IdEmpresa);
if (empresa is null)
{
    Console.WriteLine("FAIL: empresa 41 no está en ecom. Corré sync empresa/catalog primero.");
    return 1;
}

empresa.Activo = true;
empresa.UltModificacion = now;
if (string.IsNullOrWhiteSpace(empresa.RazonSocial) && string.IsNullOrWhiteSpace(empresa.NombreFantasia))
    empresa.NombreFantasia = "Casa Imperial";

var cfg = await db.EmpresaConfiguracionesEcommerce.FirstOrDefaultAsync(c => c.ID == IdEmpresa);
if (cfg is null)
{
    cfg = new Datamex.ECommerce.Domain.Entities.EmpresaConfiguracionEcommerce { ID = IdEmpresa };
    db.EmpresaConfiguracionesEcommerce.Add(cfg);
}

cfg.Activo = true;
cfg.SlugTienda = Slug;
cfg.NombreTiendaDisplay = string.IsNullOrWhiteSpace(cfg.NombreTiendaDisplay)
    ? (empresa.NombreFantasia ?? empresa.RazonSocial ?? "Casa Imperial")
    : cfg.NombreTiendaDisplay;
cfg.ModoIntegracion = (int)IntegrationMode.Integrated;
cfg.UsarListaPrecioCuentaErp = true;

// Alinear depósito e-com al que tiene stock sincronizado (espejo ERP).
var bestDep = await db.Stocks.AsNoTracking()
    .Where(s => s.IDEmpresa == IdEmpresa && s.Activo && s.TotalExistencia > 0)
    .GroupBy(s => s.IDDeposito)
    .Select(g => new { Dep = g.Key, N = g.Count() })
    .OrderByDescending(x => x.N)
    .FirstOrDefaultAsync();
if (bestDep is not null && cfg.IDDeposito != bestDep.Dep)
{
    Console.WriteLine($"  deposito: {cfg.IDDeposito} → {bestDep.Dep} ({bestDep.N} filas con existencia)");
    cfg.IDDeposito = bestDep.Dep;
}

cfg.UltModificacion = now;
cfg.UserName = "configure-ci";
Console.WriteLine($"  config: slug={cfg.SlugTienda} display={cfg.NombreTiendaDisplay} mode=Integrated dep={cfg.IDDeposito}");

var listas = await db.ListasPrecio.Where(l => l.IDEmpresa == IdEmpresa && l.Activo).ToListAsync();

// Ranking por cantidad de precios simples (>0). La lista ERP "ecommerce" (#483)
// a veces viene casi vacía; la vitrina necesita la lista operativa con cobertura.
var rankedAll = await db.DetallesListaPrecio.AsNoTracking()
    .Where(d => d.IDEmpresa == IdEmpresa && d.Activo && d.Precio > 0)
    .GroupBy(d => d.IDListaPrecio)
    .Select(g => new { Id = g.Key, N = g.Count() })
    .OrderByDescending(x => x.N)
    .ToListAsync();
var ranked = rankedAll.FirstOrDefault();
const int minUsefulPrecios = 100;

async Task<int> CountPreciosAsync(long idLista) =>
    await db.DetallesListaPrecio.AsNoTracking()
        .CountAsync(d => d.IDEmpresa == IdEmpresa && d.IDListaPrecio == idLista && d.Activo && d.Precio > 0);

if (listas.Count > 0 && ranked is not null)
{
    // Desmarcar listas e-com esqueléticas si hay otra con cobertura real.
    foreach (var sparse in listas.Where(l => l.EsListaPrecioEcommerce).ToList())
    {
        var n = await CountPreciosAsync(sparse.ID);
        if (sparse.ID == ranked.Id) continue;
        if (n < minUsefulPrecios && ranked.N >= minUsefulPrecios)
        {
            sparse.EsListaPrecioEcommerce = false;
            sparse.UltModificacion = now;
            sparse.UserName = "configure-ci";
            Console.WriteLine(
                $"  lista e-com desmarcada (poca cobertura): id={sparse.ID} nro={sparse.NroListaPrecio} '{sparse.Descripcion}' ({n} precios)");
        }
    }

    var pick = listas.First(l => l.ID == ranked.Id);
    var before = pick.EsListaPrecioEcommerce && pick.Estado == 2 && pick.CanalEcommerce == "BOTH";
    pick.EsListaPrecioEcommerce = true;
    pick.CanalEcommerce = "BOTH";
    pick.Estado = 2;
    pick.Activo = true;
    pick.UltModificacion = now;
    pick.UserName = "configure-ci";
    if (!before)
        Console.WriteLine(
            $"  lista e-com operativa: id={pick.ID} nro={pick.NroListaPrecio} '{pick.Descripcion}' ({ranked.N} precios)");
}
else if (listas.Count > 0)
{
    var pick = listas.OrderBy(l => l.NroListaPrecio).First();
    pick.EsListaPrecioEcommerce = true;
    pick.CanalEcommerce = "BOTH";
    pick.Estado = 2;
    pick.Activo = true;
    pick.UltModificacion = now;
    pick.UserName = "configure-ci";
    Console.WriteLine($"  lista e-com fallback: id={pick.ID} nro={pick.NroListaPrecio} '{pick.Descripcion}'");
}

var ecomListas = listas.Where(l => l.EsListaPrecioEcommerce).ToList();
foreach (var l in ecomListas)
{
    var changed = false;
    if (l.Estado != 2) { l.Estado = 2; changed = true; }
    if (string.IsNullOrWhiteSpace(l.CanalEcommerce) || l.CanalEcommerce is not ("B2C" or "B2B" or "BOTH"))
    {
        l.CanalEcommerce = "BOTH";
        changed = true;
    }
    if (!l.Activo) { l.Activo = true; changed = true; }
    if (changed)
    {
        l.UltModificacion = now;
        l.UserName = "configure-ci";
        Console.WriteLine($"  lista ajustada: id={l.ID} nro={l.NroListaPrecio} estado=2 canal={l.CanalEcommerce}");
    }
}

await db.SaveChangesAsync();

// Publicar catálogo activo a vitrina
var published = await db.Articulos
    .Where(a => a.IDEmpresa == IdEmpresa && a.Activo && !a.ActivoEcommerce)
    .ExecuteUpdateAsync(s => s
        .SetProperty(a => a.ActivoEcommerce, true)
        .SetProperty(a => a.EstadoEcommerce, "activo")
        .SetProperty(a => a.UltModificacion, now));
Console.WriteLine($"  articulos publicados a vitrina: {published}");

await DemoSeed.EnsureBackofficeRbacAsync(db, IdEmpresa, log, CancellationToken.None);

var arts = await db.Articulos.CountAsync(a => a.IDEmpresa == IdEmpresa);
var artsEcom = await db.Articulos.CountAsync(a => a.IDEmpresa == IdEmpresa && a.ActivoEcommerce);
var listasN = await db.ListasPrecio.CountAsync(l => l.IDEmpresa == IdEmpresa);
var listasEcom = await db.ListasPrecio.CountAsync(l => l.IDEmpresa == IdEmpresa && l.EsListaPrecioEcommerce && l.Estado == 2);
var detSimple = await db.DetallesListaPrecio.CountAsync(d => d.IDEmpresa == IdEmpresa);
var detCv = await db.DetallesListaPrecioCondicionVenta.CountAsync(d => d.IDEmpresa == IdEmpresa);
var stock = await db.Stocks.CountAsync(s => s.IDEmpresa == IdEmpresa);
var admins = await db.AdminUsuarios.AsNoTracking().Where(u => u.IDEmpresa == IdEmpresa).ToListAsync();

Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] DONE");
Console.WriteLine($"  arts={arts} artsEcom={artsEcom} listas={listasN} listasEcomVigentes={listasEcom}");
Console.WriteLine($"  detalleSimple={detSimple} detalleCV={detCv} stock={stock}");
Console.WriteLine($"  admin login: slug={Slug} / admin / Admin12!");
foreach (var u in admins)
    Console.WriteLine($"    usuario={u.Usuario} activo={u.Activo}");
Console.WriteLine("CONFIGURE_COMPLETE");
return 0;

static string Mask(string? cs)
{
    if (string.IsNullOrWhiteSpace(cs)) return "(empty)";
    return System.Text.RegularExpressions.Regex.Replace(
        cs, @"Password=[^;]*", "Password=***", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
}

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
        if (Directory.Exists(Path.Combine(dir.FullName, "src", "Datamex.ECommerce.Api")))
            return dir.FullName;
        dir = dir.Parent;
    }
    throw new DirectoryNotFoundException("No se encontró raíz E-commerce.");
}
