using System.Text;

namespace Property.Infrastructure.Billing;

/// <summary>Minimal PDF 1.4 builder for cash receipts (BIL-013). No external PDF library.</summary>
public static class ReceiptPdfBuilder
{
    public static byte[] Build(
        string propertyName,
        string unitCode,
        decimal amount,
        DateTime paidAtUtc,
        string? reference,
        string methodLabel,
        Guid paymentId)
    {
        var lines = new[]
        {
            "RECIBO DE CAJA",
            propertyName,
            $"Unidad: {unitCode}",
            $"Monto: {amount:0.00}",
            $"Fecha: {paidAtUtc:yyyy-MM-dd HH:mm} UTC",
            $"Metodo: {methodLabel}",
            $"Referencia: {(string.IsNullOrWhiteSpace(reference) ? "-" : reference)}",
            $"Pago Id: {paymentId:N}",
        };

        var content = new StringBuilder();
        content.Append("BT /F1 14 Tf 50 740 Td 16 TL\n");
        for (var i = 0; i < lines.Length; i++)
        {
            if (i == 0)
                content.Append($"({Escape(lines[i])}) Tj T*\n");
            else
            {
                if (i == 1)
                    content.Append("/F1 11 Tf\n");
                content.Append($"({Escape(lines[i])}) Tj T*\n");
            }
        }

        content.Append("ET");
        var stream = content.ToString();
        var streamBytes = Encoding.ASCII.GetBytes(stream);

        var objects = new List<string>
        {
            "1 0 obj<< /Type /Catalog /Pages 2 0 R >>endobj\n",
            "2 0 obj<< /Type /Pages /Kids [3 0 R] /Count 1 >>endobj\n",
            "3 0 obj<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources<< /Font<< /F1 5 0 R >> >> >>endobj\n",
            $"4 0 obj<< /Length {streamBytes.Length} >>stream\n{stream}\nendstream\nendobj\n",
            "5 0 obj<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>endobj\n",
        };

        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        foreach (var obj in objects)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString()));
            sb.Append(obj);
        }

        var xrefPos = Encoding.ASCII.GetByteCount(sb.ToString());
        sb.Append($"xref\n0 {objects.Count + 1}\n");
        sb.Append("0000000000 65535 f \n");
        for (var i = 1; i <= objects.Count; i++)
            sb.Append($"{offsets[i]:D10} 00000 n \n");
        sb.Append($"trailer<< /Size {objects.Count + 1} /Root 1 0 R >>\n");
        sb.Append($"startxref\n{xrefPos}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private static string Escape(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (c is '(' or ')' or '\\')
                sb.Append('\\');
            // Keep Latin-1 printable; replace others.
            if (c > 127)
                sb.Append('?');
            else
                sb.Append(c);
        }

        return sb.ToString();
    }
}
