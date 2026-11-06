namespace Property.Application.Features.Billing.BreB;

/// <summary>Alphanumeric Bre-B reference, at most 18 characters: unit code plus YYYYMM.</summary>
public static class AdminFeeQrReference
{
    public static string FromUnitPeriod(string unitCode, int year, int month)
    {
        var period = $"{year:0000}{month:00}";
        var unit = new string(unitCode
            .Where(static c => char.IsLetterOrDigit(c) && c < 128)
            .ToArray());
        var room = 18 - period.Length;
        if (unit.Length > room)
            unit = unit[^room..];
        if (unit.Length == 0)
            unit = "U";
        return unit + period;
    }
}
