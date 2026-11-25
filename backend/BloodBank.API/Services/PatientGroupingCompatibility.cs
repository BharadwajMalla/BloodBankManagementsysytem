using BloodBank.API.Models;

namespace BloodBank.API.Services;

public static class PatientGroupingCompatibility
{
    public static bool IsCompatible(PatientGroupingRecord record, string requestedBloodGroup)
    {
        var requiredResults = new[]
        {
            record.AntiA, record.AntiB, record.AntiAb, record.AntiD, record.LectinA1,
            record.LectinH, record.ACells, record.BCells, record.OCells, record.DuTest,
            record.IrregularAntibodies, record.AutoAntibodies
        };
        if (requiredResults.Any(result =>
            string.IsNullOrWhiteSpace(result)
            || string.Equals(result, "Not tested", StringComparison.OrdinalIgnoreCase)))
            return false;

        if (!string.Equals(record.CrossmatchSaline, "Compatible", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(record.CrossmatchAlbumin, "Compatible", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(record.CrossmatchAhg, "Compatible", StringComparison.OrdinalIgnoreCase))
            return false;

        var bloodGroup = record.BloodGroup.Trim().ToUpperInvariant();
        var rhType = record.RhType.Trim();
        var rhSign = rhType.Equals("Positive", StringComparison.OrdinalIgnoreCase) || rhType == "+"
            ? "+"
            : rhType.Equals("Negative", StringComparison.OrdinalIgnoreCase) || rhType == "-"
                ? "-"
                : null;
        if (rhSign == null) return false;

        var groupHasRhSign = bloodGroup.EndsWith("+", StringComparison.Ordinal)
            || bloodGroup.EndsWith("-", StringComparison.Ordinal);
        var combinedGroup = groupHasRhSign
            ? bloodGroup.EndsWith(rhSign, StringComparison.Ordinal) ? bloodGroup : null
            : bloodGroup + rhSign;

        return combinedGroup != null
            && string.Equals(combinedGroup, requestedBloodGroup.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
