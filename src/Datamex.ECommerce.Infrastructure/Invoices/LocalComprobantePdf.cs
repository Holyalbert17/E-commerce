using System.Globalization;
using System.Text;

namespace Datamex.ECommerce.Infrastructure.Invoices;

/// <summary>
/// Comprobante PDF mínimo (sin dependencias) para standalone / cuando Pentaho no está disponible.
/// No reemplaza factura fiscal electrónica del ERP; sirve como comprobante operativo descargable.
/// </summary>
internal static class LocalComprobantePdf
{
    public sealed record Line(string Sku, string Nombre, int Cantidad, decimal Precio, decimal Monto);

    public sealed record Model(
        string EmpresaNombre,
        long NroPedido,
        long NroFactura,
        DateTime Fecha,
        string RazonSocial,
        string Documento,
        string TipoEntrega,
        string Observacion,
        IReadOnlyList<Line> Items,
        decimal MontoTotal,
        decimal MontoIva,
        string Titulo = "COMPROBANTE E-COMMERCE",
        string EtiquetaNumero = "Factura N");

    public static byte[] Build(Model m)
    {
        var lines = new List<string>
        {
            m.Titulo,
            m.EmpresaNombre,
            "",
            $"{m.EtiquetaNumero}: {m.NroFactura}",
            $"Pedido N: {m.NroPedido}",
            $"Fecha: {m.Fecha.ToLocalTime():dd/MM/yyyy HH:mm}",
            "",
            $"Cliente: {m.RazonSocial}",
            $"Documento: {m.Documento}",
            $"Entrega: {m.TipoEntrega}",
            "",
            "Detalle",
            "------------------------------------------------",
        };

        foreach (var it in m.Items)
        {
            var name = Trunc(it.Nombre, 42);
            var sku = string.IsNullOrWhiteSpace(it.Sku) ? "" : $"[{it.Sku}] ";
            lines.Add($"{sku}{name}");
            lines.Add(
                $"  x{it.Cantidad}  {Fmt(it.Precio)}  =  {Fmt(it.Monto)}");
        }

        lines.Add("------------------------------------------------");
        if (m.MontoIva > 0)
            lines.Add($"IVA (ref.): {Fmt(m.MontoIva)}");
        lines.Add($"TOTAL: {Fmt(m.MontoTotal)} Gs");
        if (!string.IsNullOrWhiteSpace(m.Observacion))
        {
            lines.Add("");
            lines.Add($"Obs: {Trunc(m.Observacion, 80)}");
        }

        lines.Add("");
        lines.Add("Documento generado por Datamex E-commerce.");
        lines.Add("Si hay facturacion fiscal ERP/Pentaho, prevalece ese PDF.");

        return EncodeSimplePdf(lines);
    }

    private static string Fmt(decimal v) =>
        v.ToString("N0", CultureInfo.GetCultureInfo("es-PY"));

    private static string Trunc(string s, int max) =>
        string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s[..(max - 1)] + "…");

    /// <summary>PDF 1.4 mínimo con una página de texto Helvetica.</summary>
    private static byte[] EncodeSimplePdf(IReadOnlyList<string> textLines)
    {
        var content = new StringBuilder();
        content.AppendLine("BT");
        content.AppendLine("/F1 10 Tf");
        content.AppendLine("50 800 Td");
        content.AppendLine("14 TL");
        for (var i = 0; i < textLines.Count; i++)
        {
            var t = EscapePdf(textLines[i]);
            if (i == 0)
                content.AppendLine($"({t}) Tj");
            else
                content.AppendLine($"T* ({t}) Tj");
        }
        content.AppendLine("ET");
        var stream = Encoding.ASCII.GetBytes(content.ToString());

        var objects = new List<byte[]>();
        objects.Add(Encoding.ASCII.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"));
        objects.Add(Encoding.ASCII.GetBytes("<< /Type /Pages /Kids [3 0 R] /Count 1 >>"));
        objects.Add(Encoding.ASCII.GetBytes(
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>"));
        objects.Add(Encoding.ASCII.GetBytes($"<< /Length {stream.Length} >>\nstream\n")
            .Concat(stream)
            .Concat(Encoding.ASCII.GetBytes("\nendstream"))
            .ToArray());
        objects.Add(Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"));

        using var ms = new MemoryStream();
        void W(string s)
        {
            var b = Encoding.ASCII.GetBytes(s);
            ms.Write(b, 0, b.Length);
        }

        W("%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(ms.Position);
            W($"{i + 1} 0 obj\n");
            ms.Write(objects[i], 0, objects[i].Length);
            W("\nendobj\n");
        }

        var xref = ms.Position;
        W($"xref\n0 {objects.Count + 1}\n");
        W("0000000000 65535 f \n");
        for (var i = 1; i <= objects.Count; i++)
            W($"{offsets[i]:D10} 00000 n \n");
        W($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\n");
        W($"startxref\n{xref}\n%%EOF\n");
        return ms.ToArray();
    }

    private static string EscapePdf(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (ch is '(' or ')' or '\\')
                sb.Append('\\').Append(ch);
            else if (ch < 32 || ch > 126)
                sb.Append('?'); // Helvetica ASCII-only
            else
                sb.Append(ch);
        }
        return sb.ToString();
    }
}
