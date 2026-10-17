namespace Datamex.ECommerce.Application.Options;

/// <summary>
/// Búsqueda de alto rendimiento: índice local (default) + Redis opcional + OpenSearch opcional.
/// Standalone funciona sin infraestructura externa; Integrated engancha Redis/OpenSearch cuando hay URL.
/// </summary>
public class SearchOptions
{
    public const string SectionName = "Search";

    /// <summary>Local | Redis | OpenSearch (cadena: siempre Local como fallback).</summary>
    public string Engine { get; set; } = "Local";

    public bool Enabled { get; set; } = true;

    /// <summary>TTL de resultados en Redis / memoria servidor (segundos).</summary>
    public int ResultCacheSeconds { get; set; } = 60;

    /// <summary>Intervalo de rebuild del índice local por tenant (minutos).</summary>
    public int IndexRefreshMinutes { get; set; } = 5;

    /// <summary>Máximo de documentos en snapshot para prefetch IndexedDB.</summary>
    public int SnapshotMaxDocs { get; set; } = 2000;

    /// <summary>Connection string Redis (opcional). Vacío = solo MemoryCache.</summary>
    public string? RedisConnection { get; set; }

    /// <summary>Base URL OpenSearch/Elasticsearch (opcional), ej. http://localhost:9200</summary>
    public string? OpenSearchUrl { get; set; }

    /// <summary>Prefijo índice OS, se concatena con idEmpresa.</summary>
    public string OpenSearchIndexPrefix { get; set; } = "datamex-ecom-";
}
