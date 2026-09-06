using System;
using System.Collections;

namespace Samples.MinimalApis.ApiValidators.RuleBuilders;

internal sealed partial class RuleBuilder<TArgument, TProperty>
{
    public IRuleBuilder<TArgument, TProperty> NotEmpty()
        => AddRule(instance => IsEmptyValue(GetPropertyValue(instance))
            ? Fail($"{GetPropertyName()} must not be empty")
            : null);

    public IRuleBuilder<TArgument, TProperty> Empty()
        => AddRule(instance => IsEmptyValue(GetPropertyValue(instance))
            ? null
            : Fail($"{GetPropertyName()} must be empty"));

    private static bool IsEmptyValue(object? value)
    {
        if (value is null)
        {
            return true;
        }

        if (value is string @string)
        {
            return string.IsNullOrWhiteSpace(@string);
        }

        if (value is IEnumerable enumerable)
        {
            return !HasItems(enumerable);
        }

        var type = value.GetType();
        return type.IsValueType && value.Equals(Activator.CreateInstance(type));
    }

    public IRuleBuilder<TArgument, TProperty> MinimumLength(int minLength)
        => AddRule(instance =>
        {
            var propertyValue = GetPropertyValue(instance);
            if (propertyValue is not IEnumerable propertyValueAsEnumerable)
            {
                return null;
            }

            return GetCount(propertyValueAsEnumerable) < minLength
                ? Fail($"{GetPropertyName()} length must be at least {minLength}")
                : null;
        });

    public IRuleBuilder<TArgument, TProperty> MaximumLength(int maxLength)
        => AddRule(instance =>
        {
            var propertyValue = GetPropertyValue(instance);
            if (propertyValue is not IEnumerable propertyValueAsEnumerable)
            {
                return null;
            }

            return GetCount(propertyValueAsEnumerable) > maxLength
                ? Fail($"{GetPropertyName()} length must not exceed {maxLength}")
                : null;
        });

    public IRuleBuilder<TArgument, TProperty> Length(int exactLength)
        => AddRule(instance =>
        {
            var propertyValue = GetPropertyValue(instance);
            if (propertyValue is not IEnumerable propertyValueAsEnumerable)
            {
                return null;
            }

            return GetCount(propertyValueAsEnumerable) != exactLength
                ? Fail($"{GetPropertyName()} length must be exactly {exactLength}")
                : null;
        });

    public IRuleBuilder<TArgument, TProperty> Length(int minLength, int maxLength)
        => AddRule(instance =>
        {
            var propertyValue = GetPropertyValue(instance);
            if (propertyValue is not IEnumerable propertyValueAsEnumerable)
            {
                return null;
            }

            var count = GetCount(propertyValueAsEnumerable);
            return count < minLength || count > maxLength
                ? Fail($"{GetPropertyName()} length must be between {minLength} and {maxLength}")
                : null;
        });

    private static bool HasItems(IEnumerable enumerable)
    {
        foreach (var _ in enumerable)
        {
            return true;
        }

        return false;
    }

    private static int GetCount(IEnumerable enumerable)
        => enumerable switch
        {
            string @string => @string.Length,
            ICollection collection => collection.Count,
            _ => GetCountSlow(enumerable)
        };

    private static int GetCountSlow(IEnumerable enumerable)
    {
        var count = 0;
        foreach (var _ in enumerable)
        {
            count++;
        }

        return count;
    }
}
