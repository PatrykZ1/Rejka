using System.Text.RegularExpressions;

namespace backend.Infrastructure.Analysis;

internal static class PolishPlateNumberFormat
{
    private static readonly Regex StandardPattern = new(
        @"\A[ABCDEFGIJKLMNOPRSTVWXYZ][A-Z]{1,2}[0-9ACEFGHJKLMNPQRSTUVWXY]{4,5}\z",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex IndividualPattern = new(
        @"\A[ABCDEFGIJKLMNOPRSTVWXYZ][0-9](?:[A-Z]{3,5}|[A-Z]{2,4}[0-9]|[A-Z]{1,3}[0-9]{2})\z",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool IsValid(string registration) =>
        StandardPattern.IsMatch(registration) || IndividualPattern.IsMatch(registration);
}
