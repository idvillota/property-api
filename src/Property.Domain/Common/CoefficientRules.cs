namespace Property.Domain.Common;

public static class CoefficientRules
{
    public const decimal Total = 100m;
    public const int DecimalPlaces = 5;

    public static bool IsValid(decimal coefficient) =>
        coefficient >= 0m && coefficient <= Total;

    public static void EnsureValid(decimal coefficient)
    {
        if (!IsValid(coefficient))
            throw new InvalidOperationException($"Coefficient must be between 0 and {Total:0.#####}.");
    }
}
