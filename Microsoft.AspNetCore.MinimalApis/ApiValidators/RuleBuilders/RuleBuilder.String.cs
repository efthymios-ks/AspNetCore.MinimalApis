using System.Text.RegularExpressions;

namespace Samples.MinimalApis.ApiValidators.RuleBuilders;

internal sealed partial class RuleBuilder<TArgument, TProperty>
{
    public IRuleBuilder<TArgument, TProperty> Matches(
        string pattern,
        RegexOptions options
            = RegexOptions.Compiled
            | RegexOptions.CultureInvariant
            | RegexOptions.IgnoreCase
            | RegexOptions.Multiline
    )
    {
        ArgumentNullException.ThrowIfNull(pattern);

        return AddRule(instance =>
        {
            var propertyValue = GetPropertyValue(instance);
            if (propertyValue is not string propertyValueAsString)
            {
                return null;
            }

            return !Regex.IsMatch(propertyValueAsString, pattern, options)
                ? Fail($"{GetPropertyName()} is not in the correct format")
                : null;
        });
    }

    public IRuleBuilder<TArgument, TProperty> Contains(
        string value,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return AddRule(instance =>
        {
            var propertyValue = GetPropertyValue(instance);
            if (propertyValue is not string propertyValueAsString)
            {
                return null;
            }

            return !propertyValueAsString.Contains(value, comparison)
                ? Fail($"{GetPropertyName()} must contain '{value}'")
                : null;
        });
    }

    public IRuleBuilder<TArgument, TProperty> StartsWith(
        string value,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return AddRule(instance =>
        {
            var propertyValue = GetPropertyValue(instance);
            if (propertyValue is not string propertyValueAsString)
            {
                return null;
            }

            return !propertyValueAsString.StartsWith(value, comparison)
                ? Fail($"{GetPropertyName()} must start with '{value}'")
                : null;
        });
    }

    public IRuleBuilder<TArgument, TProperty> EndsWith(
        string value,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return AddRule(instance =>
        {
            var propertyValue = GetPropertyValue(instance);
            if (propertyValue is not string propertyValueAsString)
            {
                return null;
            }

            return !propertyValueAsString.EndsWith(value, comparison)
                ? Fail($"{GetPropertyName()} must end with '{value}'")
                : null;
        });
    }
}
