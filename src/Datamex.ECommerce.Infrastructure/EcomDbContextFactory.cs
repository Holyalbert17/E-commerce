using System;
using Datamex.ECommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Datamex.ECommerce.Infrastructure;

/// <summary>
/// Permite <c>dotnet ef</c> sin arrancar la API.
/// Por defecto usa Postgres (remoto o local). SQLite solo si
/// <c>Database__Provider=Sqlite</c> o la CS empieza con <c>Data Source=</c>.
/// Override: <c>EcomDbConnection</c> (+ opcional <c>Database__Provider</c>).
/// </summary>
public sealed class EcomDbContextFactory : IDesignTimeDbContextFactory<EcomDbContext>
{
    public EcomDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("EcomDbConnection");
        if (string.IsNullOrWhiteSpace(conn))
        {
            throw new InvalidOperationException(
                "Definí la variable de entorno EcomDbConnection antes de usar dotnet ef. " +
                "Ejemplo Postgres: Host=localhost;Port=5432;Database=datamex_ecom;Username=ecom;Password=...;SSL Mode=Prefer;Search Path=dbo,public. " +
                "Ejemplo SQLite: Data Source=App_Data/ecom.db y Database__Provider=Sqlite.");
        }

        var provider = Environment.GetEnvironmentVariable("Database__Provider")
                       ?? Environment.GetEnvironmentVariable("Database:Provider")
                       ?? "Postgres";

        var builder = new DbContextOptionsBuilder<EcomDbContext>();
        var useSqlite = string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase)
                        || conn.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase);

        if (useSqlite)
            builder.UseSqlite(conn);
        else
            builder.UseNpgsql(conn, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo"));

        return new EcomDbContext(builder.Options);
    }
}
