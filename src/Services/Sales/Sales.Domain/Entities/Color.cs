using System.Text.RegularExpressions;

namespace Sales.Domain;

public sealed partial class Color : Entity<Guid>
{
    private Color() { }

    private Color(Guid id, string colorCode, string name, string? hexCode)
    {
        Id = id;
        ColorCode = ProductCodeRules.Normalize(colorCode, "Color code");
        Name = string.IsNullOrWhiteSpace(name) ? throw new DomainException("Color name is required.") : name.Trim();
        HexCode = NormalizeHexCode(hexCode);
    }

    public string ColorCode { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? HexCode { get; private set; }

    public static Color Create(Guid id, string colorCode, string name, string? hexCode)
    {
        if (id == Guid.Empty) throw new DomainException("Color id is required.");
        return new Color(id, colorCode, name, hexCode);
    }

    private static string? NormalizeHexCode(string? hexCode)
    {
        if (string.IsNullOrWhiteSpace(hexCode)) return null;

        var normalized = hexCode.Trim().ToUpperInvariant();
        if (!HexCodeRegex().IsMatch(normalized)) throw new DomainException("Color hex code must use #RRGGBB format.");
        return normalized;
    }

    [GeneratedRegex("^#[0-9A-F]{6}$")]
    private static partial Regex HexCodeRegex();
}
