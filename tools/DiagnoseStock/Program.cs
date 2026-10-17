using Datamex.ECommerce.Infrastructure;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var repoRoot = FindRepoRoot();
var apiPath = Path.Combine(repoRoot, "src", "Datamex.ECommerce.Api");
Directory.SetCurrentDirectory(apiPath);
var config = new ConfigurationBuilder()
    .SetBasePath(apiPath)
    .AddJsonFile("appsettings.json", true)
    .AddJsonFile("appsettings.Development.json", true)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
services.AddSingleton<IConfiguration>(config);
services.AddECommerceInfrastructure(config);
await using var sp = services.BuildServiceProvider();
await using var scope = sp.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<EcomDbContext>();

const long e = 41;
var cfg = await db.EmpresaConfiguracionesEcommerce.AsNoTracking().FirstAsync(c => c.ID == e);
Console.WriteLine($"IDDeposito={cfg.IDDeposito} ControlaStock={cfg.ControlaStockEnCarrito} Modo={cfg.ModoIntegracion}");

var stockAll = await db.Stocks.CountAsync(s => s.IDEmpresa == e && s.Activo);
var stockDep = await db.Stocks.CountAsync(s => s.IDEmpresa == e && s.Activo && s.IDDeposito == cfg.IDDeposito);
var stockDepPos = await db.Stocks.CountAsync(s => s.IDEmpresa == e && s.Activo && s.IDDeposito == cfg.IDDeposito && s.TotalExistencia > 0);
Console.WriteLine($"stock_all={stockAll} stock_dep={stockDep} stock_dep_pos={stockDepPos}");

var byDep = await db.Stocks.AsNoTracking()
    .Where(s => s.IDEmpresa == e && s.Activo)
    .GroupBy(s => s.IDDeposito)
    .Select(g => new { Dep = g.Key, N = g.Count(), Sum = g.Sum(x => x.TotalExistencia) })
    .OrderByDescending(x => x.N)
    .Take(8)
    .ToListAsync();
foreach (var row in byDep)
    Console.WriteLine($"  dep={row.Dep} n={row.N} sumEx={row.Sum:0.##}");

var deps = await (
    from d in db.Depositos.AsNoTracking()
    join t in db.TiposDeposito.AsNoTracking() on d.IDTipoDeposito equals t.ID into tj
    from t in tj.DefaultIfEmpty()
    where d.IDEmpresa == e || byDep.Select(x => x.Dep).Contains(d.ID)
    select new { d.ID, d.CodDeposito, d.DescripcionDeposito, d.IDEmpresa, Normal = t != null && t.Normal, d.EsDepositoEcommerce }
).Take(20).ToListAsync();
foreach (var d in deps)
    Console.WriteLine($"  deposito id={d.ID} emp={d.IDEmpresa} cod={d.CodDeposito} '{d.DescripcionDeposito}' normal={d.Normal} ecom={d.EsDepositoEcommerce}");

// Sample: arts with price and stock
var sample = await (
    from a in db.Articulos.AsNoTracking()
    where a.IDEmpresa == e && a.ActivoEcommerce
    join s in db.Stocks.AsNoTracking() on a.ID equals s.IDArticulo
    where s.IDEmpresa == e && s.Activo && s.TotalExistencia > 0
    select new { a.ID, s.IDDeposito, s.TotalExistencia }
).Take(5).ToListAsync();
Console.WriteLine("sample arts with stock>0:");
foreach (var s in sample)
    Console.WriteLine($"  art={s.ID} dep={s.IDDeposito} ex={s.TotalExistencia}");

// Misma query que InventoryService (depósito config + Normal).
var cfg2 = await db.EmpresaConfiguracionesEcommerce.AsNoTracking().FirstAsync(c => c.ID == e);
var probeIds = sample.Select(x => x.ID).Distinct().Take(5).ToList();
if (probeIds.Count == 0)
    probeIds = await db.Articulos.AsNoTracking().Where(a => a.IDEmpresa == e && a.ActivoEcommerce).Select(a => a.ID).Take(5).ToListAsync();

var inv = await (
    from s in db.Stocks.AsNoTracking()
    join d in db.Depositos.AsNoTracking() on s.IDDeposito equals d.ID
    join t in db.TiposDeposito.AsNoTracking() on d.IDTipoDeposito equals t.ID
    where s.IDEmpresa == e && s.Activo && !s.ExistenciaCongelada
          && probeIds.Contains(s.IDArticulo) && t.Normal
          && s.IDDeposito == cfg2.IDDeposito
    group s by s.IDArticulo into g
    select new { Id = g.Key, Ex = g.Sum(x => x.TotalExistencia), Res = g.Sum(x => (double)x.TotalReserva) }
).ToListAsync();
Console.WriteLine($"inventory-query dep={cfg2.IDDeposito} hits={inv.Count}");
foreach (var row in inv)
    Console.WriteLine($"  art={row.Id} libre={Math.Max(0, (decimal)row.Ex - (decimal)row.Res):0.##}");

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
        if (Directory.Exists(Path.Combine(dir.FullName, "src", "Datamex.ECommerce.Api")))
            return dir.FullName;
        dir = dir.Parent;
    }
    throw new DirectoryNotFoundException();
}
