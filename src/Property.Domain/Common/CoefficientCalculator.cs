using Property.Domain.Entities;

namespace Property.Domain.Common;

/// <summary>
/// Distributes coefficients so active units sum to <see cref="CoefficientRules.Total"/>
/// proportional to private built area (m²).
/// </summary>
public static class CoefficientCalculator
{
    public static void ApplyAreaProportional(IList<Unit> activeUnits)
    {
        if (activeUnits.Count == 0)
            return;

        var totalArea = activeUnits.Sum(u => u.PrivateBuiltAreaM2);
        if (totalArea <= 0m)
        {
            foreach (var unit in activeUnits)
                unit.Coefficient = 0m;
            return;
        }

        decimal assigned = 0m;
        for (var i = 0; i < activeUnits.Count; i++)
        {
            var unit = activeUnits[i];
            if (i == activeUnits.Count - 1)
            {
                unit.Coefficient = CoefficientRules.Total - assigned;
                if (unit.Coefficient < 0m)
                    unit.Coefficient = 0m;
                continue;
            }

            var raw = unit.PrivateBuiltAreaM2 / totalArea * CoefficientRules.Total;
            unit.Coefficient = Math.Round(raw, CoefficientRules.DecimalPlaces, MidpointRounding.AwayFromZero);
            assigned += unit.Coefficient;
        }
    }
}
