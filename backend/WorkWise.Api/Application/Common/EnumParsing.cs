namespace WorkWise.Api.Application.Common;

public static class EnumParsing
{
    /// <summary>
    /// Parses an enum by name only (case-insensitive). Numeric strings such as "1" are rejected
    /// so the API contract stays string-based.
    /// </summary>
    public static bool TryParseName<TEnum>(string? value, out TEnum result) where TEnum : struct, Enum
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var trimmed = value.Trim();
        if (trimmed.Length == 0 || char.IsDigit(trimmed[0]) || trimmed[0] == '-') return false;
        return Enum.TryParse(trimmed, ignoreCase: true, out result) && Enum.IsDefined(result);
    }

    public static bool IsValidName<TEnum>(string? value) where TEnum : struct, Enum
        => TryParseName<TEnum>(value, out _);

    public static string AllowedValues<TEnum>() where TEnum : struct, Enum
        => string.Join(", ", Enum.GetNames<TEnum>());
}
