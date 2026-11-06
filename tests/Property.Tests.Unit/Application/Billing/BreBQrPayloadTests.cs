using Property.Application.Features.Billing.BreB;

namespace Property.Tests.Unit.Billing;

public sealed class BreBQrPayloadTests
{
    [Fact]
    public void Crc16_matches_the_standard_vector()
    {
        Assert.Equal("29B1", BreBQrPayload.Crc16Ccitt("123456789"));
    }

    [Fact]
    public void Build_locks_the_transfer_amount_and_key()
    {
        var payload = BreBQrPayload.Build(
            new BreBQrProfile("@localkey", 4, "José Café", "Bogotá", "11001"),
            15000m,
            "ABC123");

        Assert.Contains("010211", payload, StringComparison.Ordinal);
        Assert.Contains("0409@localkey", payload, StringComparison.Ordinal);
        Assert.Contains("540815000.00", payload, StringComparison.Ordinal);
        Assert.Contains("5909Jose Cafe", payload, StringComparison.Ordinal);
        Assert.Contains("6006Bogota", payload, StringComparison.Ordinal);
        Assert.Contains("610511001", payload, StringComparison.Ordinal);
        Assert.EndsWith(BreBQrPayload.Crc16Ccitt(payload[..^4]), payload, StringComparison.Ordinal);
        Assert.DoesNotContain("6304", payload[..^8], StringComparison.Ordinal);
    }

    [Fact]
    public void Build_rejects_a_blank_key()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            BreBQrPayload.Build(
                new BreBQrProfile("  ", 2, "Local", "Cali", ""),
                10m,
                "REF1"));

        Assert.Contains("llave", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Reference_keeps_the_unit_and_the_period_within_18_characters()
    {
        Assert.Equal("101202608", AdminFeeQrReference.FromUnitPeriod("101", 2026, 8));
        Assert.Equal("B201202608", AdminFeeQrReference.FromUnitPeriod("B-201", 2026, 8));
        Assert.Equal("345678901234202608", AdminFeeQrReference.FromUnitPeriod("XXXX12345678901234", 2026, 8));
    }
}
