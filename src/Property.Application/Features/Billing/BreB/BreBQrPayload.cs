using System.Globalization;
using System.Text;

namespace Property.Application.Features.Billing.BreB;

public sealed record BreBQrProfile(
    string Key,
    int KeyType,
    string MerchantName,
    string City,
    string PostalCode);

/// <summary>
/// Merchant QR text, same layout as HostPrintGateway <c>BreBQrPayload</c>.
/// The amount is locked inside the code.
/// </summary>
public static class BreBQrPayload
{
    public const string MissingKey = "Configura la llave Bre-B de la copropiedad antes de enviar el QR.";
    public const string MissingPlace = "Configura el nombre comercial y la ciudad para generar el QR.";

    public static string? ConfigurationError(BreBQrProfile profile)
    {
        try
        {
            ValidateProfile(profile);
            return null;
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message;
        }
    }

    public static string Build(BreBQrProfile profile, decimal amount, string reference)
    {
        ValidateProfile(profile);
        var key = RequireKey(profile.Key);
        var rounded = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (rounded <= 0m)
            throw new InvalidOperationException("El monto del QR debe ser mayor que cero.");

        var amountText = rounded.ToString("0.00", CultureInfo.InvariantCulture);
        if (amountText.Length > 13)
            throw new InvalidOperationException("El monto del QR supera el máximo permitido.");

        var merchant = MerchantText(profile.MerchantName, 25, "nombre comercial");
        var city = MerchantText(profile.City, 15, "ciudad");
        var postal = OptionalCode(profile.PostalCode, 10);
        var qrReference = RequireReference(reference);
        var keyTag = profile.KeyType.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0');

        var body = new StringBuilder();
        Append(body, "00", "01");
        Append(body, "01", "11");
        AppendTemplate(body, "26", ("00", "CO.COM.ACH.LLA"), (keyTag, key));
        AppendTemplate(body, "49", ("00", "CO.COM.ACH.RED"), ("01", "ACH"));
        AppendTemplate(body, "50", ("00", "CO.COM.ACH.CU"), ("01", key));
        Append(body, "52", "0000");
        Append(body, "53", "170");
        Append(body, "54", amountText);
        Append(body, "58", "CO");
        Append(body, "59", merchant);
        Append(body, "60", city);
        if (postal is not null)
            Append(body, "61", postal);
        AppendTemplate(body, "62", ("07", "0000"), ("08", "00"));
        AppendTemplate(body, "80", ("00", "CO.COM.ACH.CANAL"), ("01", "POS"));
        AppendTemplate(body, "81", ("00", "CO.COM.ACH.CIVA"), ("01", "02"));
        AppendTemplate(body, "82", ("00", "CO.COM.ACH.IVA"), ("01", "0"));
        AppendTemplate(body, "83", ("00", "CO.COM.ACH.BASE"), ("01", "0"));
        AppendTemplate(body, "84", ("00", "CO.COM.ACH.CINC"), ("01", "02"));
        AppendTemplate(body, "85", ("00", "CO.COM.ACH.INC"), ("01", "0"));
        AppendTemplate(body, "90", ("00", "CO.COM.ACH.TRXID"), ("01", qrReference));

        var withCrcTag = body + "6304";
        return withCrcTag + Crc16Ccitt(withCrcTag);
    }

    public static string Crc16Ccitt(string value)
    {
        var crc = 0xFFFF;
        foreach (var raw in Encoding.ASCII.GetBytes(value))
        {
            crc ^= (raw & 0xFF) << 8;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 0x8000) != 0
                    ? ((crc << 1) ^ 0x1021) & 0xFFFF
                    : (crc << 1) & 0xFFFF;
            }
        }

        return crc.ToString("X4", CultureInfo.InvariantCulture);
    }

    private static void ValidateProfile(BreBQrProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Key) || profile.KeyType is < 1 or > 5)
            throw new InvalidOperationException(MissingKey);
        RequireKey(profile.Key);
        if (string.IsNullOrWhiteSpace(profile.MerchantName) || string.IsNullOrWhiteSpace(profile.City))
            throw new InvalidOperationException(MissingPlace);
        MerchantText(profile.MerchantName, 25, "nombre comercial");
        MerchantText(profile.City, 15, "ciudad");
    }

    private static void Append(StringBuilder body, string tag, string value)
    {
        body.Append(tag);
        body.Append(value.Length.ToString("00", CultureInfo.InvariantCulture));
        body.Append(value);
    }

    private static void AppendTemplate(StringBuilder body, string tag, params (string Tag, string Value)[] fields)
    {
        var inner = new StringBuilder();
        foreach (var field in fields)
            Append(inner, field.Tag, field.Value);
        Append(body, tag, inner.ToString());
    }

    private static string RequireKey(string key)
    {
        var value = key.Trim();
        if (value.Length is < 1 or > 40 || value.Any(static c => c < 33 || c > 126))
        {
            throw new InvalidOperationException(
                "La llave Bre-B debe tener entre 1 y 40 caracteres simples, sin espacios.");
        }

        return value;
    }

    private static string RequireReference(string reference)
    {
        var value = reference.Trim();
        if (value.Length is < 1 or > 18 || value.Any(static c => !char.IsLetterOrDigit(c) || c > 127))
            throw new InvalidOperationException("La referencia del QR no es válida.");
        return value;
    }

    private static string? OptionalCode(string value, int maxLength)
    {
        var text = new string(value.Where(static c => char.IsLetterOrDigit(c) && c < 128).ToArray());
        if (text.Length == 0)
            return null;
        return text.Length <= maxLength ? text : text[..maxLength];
    }

    private static string MerchantText(string value, int maxLength, string label)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var chars = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(c) && c < 128)
            {
                chars.Append(c);
                continue;
            }

            if (chars.Length > 0 && chars[^1] != ' ')
                chars.Append(' ');
        }

        var text = string.Join(
            ' ',
            chars.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (text.Length == 0)
        {
            throw new InvalidOperationException(
                $"El {label} del local no puede usarse en el QR. Usa letras o números.");
        }

        return text.Length > maxLength ? text[..maxLength].TrimEnd() : text;
    }
}
