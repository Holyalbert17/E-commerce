namespace Datamex.ECommerce.Application.Options;

public class PentahoOptions
{
    public const string SectionName = "Pentaho";

    /// <summary>
    /// Base ERP: http://host:9099/pentaho/api/repos/:public:reports_erp:
    /// </summary>
    public string UrlReportPentaho { get; set; } =
        "http://10.99.99.21:9099/pentaho/api/repos/:public:reports_erp:";

    /// <summary>Si false, endpoints de factura responden 503 con mensaje claro.</summary>
    public bool Enabled { get; set; } = true;

    public int TimeoutSeconds { get; set; } = 60;
}
