using System.Diagnostics;
using System.Net;

var urlNoSub =
    "http://10.99.99.21:9099/pentaho/api/repos/:public:reports_erp:comercial:facturasalonelectronica.prpt/generatedContent?idpedido=23349399&idempresa=41&output-target=pageable/pdf";
var urlSub =
    "http://10.99.99.21:9099/pentaho/api/repos/:public:reports_erp:comercial:casaimperial:facturasalonelectronica.prpt/generatedContent?idpedido=23349399&idempresa=41&output-target=pageable/pdf";

async Task Probe(string label, string url)
{
    var sw = Stopwatch.StartNew();
    try
    {
#pragma warning disable SYSLIB0014
        using var wc = new WebClient();
#pragma warning restore SYSLIB0014
        var bytes = await wc.DownloadDataTaskAsync(new Uri(url));
        var head = bytes.Length >= 4 ? System.Text.Encoding.ASCII.GetString(bytes.AsSpan(0, 4)) : "?";
        Console.WriteLine($"{label}: len={bytes.Length} ms={sw.ElapsedMilliseconds} head={head}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{label}: ERR ms={sw.ElapsedMilliseconds} {ex.GetType().Name} {ex.Message}");
    }
}

await Probe("WebClient no-sub", urlNoSub);
await Probe("WebClient casaimperial", urlSub);
