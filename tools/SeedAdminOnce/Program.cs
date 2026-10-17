using Datamex.ECommerce.Infrastructure;
using Datamex.ECommerce.Infrastructure.Persistence;
using Datamex.ECommerce.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var repoRoot = FindRepoRoot();
var apiPath = Path.Combine(repoRoot, "src", "Datamex.ECommerce.Api");
Directory.SetCurrentDirectory(apiPath);

var config = new ConfigurationBuilder()
    .SetBasePath(apiPath)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

Console.WriteLine("CS=" + config.GetConnectionString("EcomDb"));
var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddSingleton<IConfiguration>(config);
services.AddECommerceInfrastructure(config);

await using var sp = services.BuildServiceProvider();
await using var scope = sp.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<EcomDbContext>();
var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("seed");
try
{
    await DemoSeed.EnsureBackofficeRbacAsync(db, 41, log, CancellationToken.None);
    var n = await db.AdminUsuarios.CountAsync();
    Console.WriteLine("OK admins=" + n);
    foreach (var u in await db.AdminUsuarios.AsNoTracking().ToListAsync())
        Console.WriteLine($"  {u.Usuario} empresa={u.IDEmpresa}");
}
catch (Exception ex)
{
    Console.WriteLine("FAIL: " + ex);
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
