using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace Datamex.ECommerce.Infrastructure.Invoices;

/// <summary>
/// El KuDE/factura salón de Pentaho imprime original (adquirente) y duplicado en la misma hoja.
/// El comprador solo debe ver "Original: ADQUIRENTE".
/// </summary>
internal static class InvoicePdfOriginalCopy
{
    public static byte[] KeepAdquirenteCopy(byte[] pdf)
    {
        if (pdf is not { Length: >= 8 } || pdf[0] != (byte)'%' || pdf[1] != (byte)'P')
            return pdf;

        try
        {
            using var input = new MemoryStream(pdf, writable: false);
            using var doc = PdfReader.Open(input, PdfDocumentOpenMode.Modify);
            if (doc.PageCount == 0)
                return pdf;

            while (doc.PageCount > 1)
                doc.Pages.RemoveAt(doc.PageCount - 1);

            var page = doc.Pages[0];
            var box = page.MediaBox;
            var height = box.Height;
            var width = box.Width;
            // Hoja A4 con original arriba y duplicado abajo (Casa Imperial y reportes salón).
            if (height >= 700)
            {
                var splitFromBottom = height * 0.51;
                using (var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append))
                {
                    // XGraphics: origen arriba-izquierda. El duplicado está en la mitad inferior.
                    gfx.DrawRectangle(XBrushes.White, 0, height - splitFromBottom, width, splitFromBottom);
                }

                var splitY = box.Y1 + splitFromBottom;
                var cropped = new PdfRectangle(new XPoint(box.X1, splitY), new XPoint(box.X2, box.Y2));
                page.MediaBox = cropped;
                page.CropBox = cropped;
                page.TrimBox = cropped;
            }

            using var output = new MemoryStream();
            doc.Save(output);
            return output.ToArray();
        }
        catch
        {
            return pdf;
        }
    }
}
