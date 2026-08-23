using System.Text;

namespace Property.Infrastructure.Services.Assemblies;

public static class MinutesPdfBuilder
{
    public static byte[] Build(string title, DateTime scheduledAtUtc, IEnumerable<string> lines)
    {
        var all = new List<string> { "ACTA DE ASAMBLEA", title, $"Fecha: {scheduledAtUtc:yyyy-MM-dd HH:mm} UTC" };
        all.AddRange(lines);
        var content = new StringBuilder();
        content.Append("BT /F1 14 Tf 50 740 Td 14 TL\n");
        for (var i = 0; i < all.Count; i++)
        {
            if (i == 1) content.Append("/F1 10 Tf\n");
            content.Append($"({Escape(all[i])}) Tj T*\n");
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
        var xref = Encoding.ASCII.GetByteCount(sb.ToString());
        sb.Append($"xref\n0 {objects.Count + 1}\n");
        sb.Append("0000000000 65535 f \n");
        for (var i = 1; i < offsets.Count; i++)
            sb.Append($"{offsets[i]:D10} 00000 n \n");
        sb.Append($"trailer<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private static string Escape(string s) =>
        s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
