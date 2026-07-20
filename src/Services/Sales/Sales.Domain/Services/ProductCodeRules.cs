using System.Text.RegularExpressions;

namespace Sales.Domain;

public static partial class ProductCodeRules
{
    public static string Normalize(string code, string fieldName)
    {
        var normalized = string.IsNullOrWhiteSpace(code)
            ? throw new DomainException($"{fieldName} is required.")
            : code.Trim().ToUpperInvariant();

        if (!CodeRegex().IsMatch(normalized))
        {
            throw new DomainException($"{fieldName} contains unsupported characters.");
        }

        return normalized;
    }

    public static string BuildSku(string productCode, string colorCode, string sizeCode)
    {
        return string.Join(
            '-',
            Normalize(productCode, "Product code"),
            Normalize(colorCode, "Color code"),
            Normalize(sizeCode, "Size code"));
    }

    [GeneratedRegex("^[A-Z0-9][A-Z0-9_-]*$")]
    private static partial Regex CodeRegex();
}
