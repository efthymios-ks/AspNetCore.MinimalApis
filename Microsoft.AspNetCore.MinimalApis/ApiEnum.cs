namespace Microsoft.AspNetCore.MinimalApis;

/// <summary>
/// Wraps an enum for Minimal-API route and query binding, parsing member names case-insensitively
/// (and by numeric value) through <see cref="IParsable{TSelf}"/>. Plain Minimal-API binding uses a
/// case-sensitive <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/>, so values such as
/// <c>"oneWay"</c> would otherwise be rejected. Converts implicitly to and from <typeparamref name="TEnum"/>.
/// </summary>
public readonly record struct ApiEnum<TEnum>(TEnum Value) : IParsable<ApiEnum<TEnum>>
    where TEnum : struct, Enum
{
    public static bool TryParse(string? s, IFormatProvider? provider, out ApiEnum<TEnum> result)
    {
        if (Enum.TryParse<TEnum>(s, ignoreCase: true, out var value))
        {
            result = new ApiEnum<TEnum>(value);
            return true;
        }

        result = default;
        return false;
    }

    public static ApiEnum<TEnum> Parse(string s, IFormatProvider? provider)
        => TryParse(s, provider, out var result)
            ? result
            : throw new FormatException($"'{s}' is not a valid {typeof(TEnum).Name}.");

    public static implicit operator TEnum(ApiEnum<TEnum> value)
        => value.Value;

    public static implicit operator ApiEnum<TEnum>(TEnum value)
        => new(value);

    public override string ToString()
        => Value.ToString();
}
