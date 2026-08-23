using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Property.Infrastructure.Payments;

/// <summary>Wompi X-Event-Checksum / signature.checksum verification (DEC-PSP / BIL-011).</summary>
public static class WompiEventSignature
{
    public static bool TryValidate(
        string rawJsonBody,
        string? checksumHeader,
        string eventsSecret,
        out string? failureReason)
    {
        failureReason = null;
        if (string.IsNullOrWhiteSpace(eventsSecret))
        {
            failureReason = "Events secret is not configured.";
            return false;
        }

        using var doc = JsonDocument.Parse(rawJsonBody);
        var root = doc.RootElement;
        if (!root.TryGetProperty("signature", out var signature))
        {
            failureReason = "Missing signature object.";
            return false;
        }

        if (!signature.TryGetProperty("properties", out var properties) ||
            properties.ValueKind != JsonValueKind.Array)
        {
            failureReason = "Missing signature.properties.";
            return false;
        }

        if (!signature.TryGetProperty("timestamp", out var timestampEl) ||
            !timestampEl.TryGetInt64(out var timestamp))
        {
            failureReason = "Missing signature.timestamp.";
            return false;
        }

        var expectedFromBody = signature.TryGetProperty("checksum", out var checksumEl)
            ? checksumEl.GetString()
            : null;
        var provided = !string.IsNullOrWhiteSpace(checksumHeader)
            ? checksumHeader.Trim()
            : expectedFromBody?.Trim();

        if (string.IsNullOrWhiteSpace(provided))
        {
            failureReason = "Missing checksum.";
            return false;
        }

        if (!root.TryGetProperty("data", out var data))
        {
            failureReason = "Missing data object.";
            return false;
        }

        var sb = new StringBuilder();
        foreach (var prop in properties.EnumerateArray())
        {
            var path = prop.GetString();
            if (string.IsNullOrWhiteSpace(path))
                continue;
            if (!TryResolvePath(data, path, out var value))
            {
                failureReason = $"Cannot resolve signature property '{path}'.";
                return false;
            }

            sb.Append(value);
        }

        sb.Append(timestamp);
        sb.Append(eventsSecret);

        var computed = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computed.ToUpperInvariant()),
                Encoding.UTF8.GetBytes(provided.ToUpperInvariant())))
        {
            failureReason = "Checksum mismatch.";
            return false;
        }

        return true;
    }

    private static bool TryResolvePath(JsonElement data, string path, out string value)
    {
        value = string.Empty;
        var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var current = data;
        foreach (var part in parts)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(part, out current))
                return false;
        }

        value = current.ValueKind switch
        {
            JsonValueKind.String => current.GetString() ?? string.Empty,
            JsonValueKind.Number => current.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            _ => current.GetRawText(),
        };
        return true;
    }
}
