using Datamex.ECommerce.Domain.Entities;
using Datamex.ECommerce.Infrastructure.Persistence;
using Datamex.ECommerce.Infrastructure.Pricing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Datamex.ECommerce.Application.Tests.Pricing;

public class ListaPrecioCuentaHelperTests
{
    private static EcomDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<EcomDbContext>()
            .UseInMemoryDatabase("lpc-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new EcomDbContext(opts);
    }

    [Fact]
    public async Task EnsureAssignment_CreatesActiveAndDeactivatesPrevious()
    {
        await using var db = CreateDb();
        const long emp = 1;
        db.ListasPrecio.AddRange(
            new ListaPrecio { ID = 10, IDEmpresa = emp, Descripcion = "A", CanalEcommerce = "B2B", Activo = true, Estado = 2 },
            new ListaPrecio { ID = 20, IDEmpresa = emp, Descripcion = "B", CanalEcommerce = "B2B", Activo = true, Estado = 2 });
        db.Cuentas.Add(new Cuenta
        {
            ID = 100,
            IDEmpresa = emp,
            NroCuenta = 1,
            RazonSocial = "Demo",
            Activo = true,
            IDListaPrecio = 10,
        });
        db.ListasPrecioCuenta.Add(new ListaPrecioCuenta
        {
            ID = 1,
            IDEmpresa = emp,
            IDCuenta = 100,
            IDListaPrecio = 10,
            Activo = true,
        });
        await db.SaveChangesAsync();

        var id = await ListaPrecioCuentaHelper.EnsureAssignmentAsync(db, emp, 100, 20, "test");

        var rows = await db.ListasPrecioCuenta.Where(x => x.IDCuenta == 100).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.False(rows.Single(x => x.IDListaPrecio == 10).Activo);
        Assert.True(rows.Single(x => x.IDListaPrecio == 20).Activo);
        Assert.Equal(20, (await db.Cuentas.FindAsync(100L))!.IDListaPrecio);
        Assert.Equal(id, rows.Single(x => x.IDListaPrecio == 20).ID);
    }

    [Fact]
    public async Task ResolveListaId_PrefersActiveHistorial()
    {
        await using var db = CreateDb();
        const long emp = 1;
        db.Cuentas.Add(new Cuenta
        {
            ID = 5,
            IDEmpresa = emp,
            NroCuenta = 5,
            RazonSocial = "X",
            Activo = true,
            IDListaPrecio = 99, // stale vs historial
        });
        db.ListasPrecioCuenta.Add(new ListaPrecioCuenta
        {
            IDEmpresa = emp,
            IDCuenta = 5,
            IDListaPrecio = 44,
            Activo = true,
            UltModificacion = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var lista = await ListaPrecioCuentaHelper.ResolveListaIdForCuentaAsync(db, emp, 5);
        Assert.Equal(44, lista);
    }
}
