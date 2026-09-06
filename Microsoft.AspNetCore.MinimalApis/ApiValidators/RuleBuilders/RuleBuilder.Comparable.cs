namespace Samples.MinimalApis.ApiValidators.RuleBuilders;

internal sealed partial class RuleBuilder<TArgument, TProperty>
{
    public IRuleBuilder<TArgument, TProperty> GreaterThan(TProperty valueToCompare)
        => AddRule(instance =>
        {
            var propertyValue = GetPropertyValue(instance);
            if (propertyValue is not IComparable<TProperty> propertyValueAsComparable)
            {
                return null;
            }

            return propertyValueAsComparable.CompareTo(valueToCompare) <= 0
                ? Fail($"{GetPropertyName()} must be greater than {valueToCompare}")
                : null;
        });

    public IRuleBuilder<TArgument, TProperty> GreaterThanOrEqualTo(TProperty valueToCompare)
        => AddRule(instance =>
        {
            var propertyValue = GetPropertyValue(instance);
            if (propertyValue is not IComparable<TProperty> propertyValueAsComparable)
            {
                return null;
            }

            return propertyValueAsComparable.CompareTo(valueToCompare) < 0
                ? Fail($"{GetPropertyName()} must be greater than or equal to {valueToCompare}")
                : null;
        });

    public IRuleBuilder<TArgument, TProperty> LessThan(TProperty valueToCompare)
        => AddRule(instance =>
        {
            var propertyValue = GetPropertyValue(instance);
            if (propertyValue is not IComparable<TProperty> propertyValueAsComparable)
            {
                return null;
            }

            return propertyValueAsComparable.CompareTo(valueToCompare) >= 0
                ? Fail($"{GetPropertyName()} must be less than {valueToCompare}")
                : null;
        });

    public IRuleBuilder<TArgument, TProperty> LessThanOrEqualTo(TProperty valueToCompare)
        => AddRule(instance =>
        {
            var propertyValue = GetPropertyValue(instance);
            if (propertyValue is not IComparable<TProperty> propertyValueAsComparable)
            {
                return null;
            }

            return propertyValueAsComparable.CompareTo(valueToCompare) > 0
                ? Fail($"{GetPropertyName()} must be less than or equal to {valueToCompare}")
                : null;
        });

    public IRuleBuilder<TArgument, TProperty> Equal(TProperty valueToCompare)
        => AddRule(instance => !Equals(GetPropertyValue(instance), valueToCompare)
            ? Fail($"{GetPropertyName()} must be equal to {valueToCompare}")
            : null);

    public IRuleBuilder<TArgument, TProperty> NotEqual(TProperty valueToCompare)
        => AddRule(instance => Equals(GetPropertyValue(instance), valueToCompare)
            ? Fail($"{GetPropertyName()} must not be equal to {valueToCompare}")
            : null);

    public IRuleBuilder<TArgument, TProperty> InclusiveBetween(TProperty from, TProperty to)
        => AddRule(instance =>
        {
            var propertyValue = GetPropertyValue(instance);
            if (propertyValue is not IComparable<TProperty> propertyValueAsComparable)
            {
                return null;
            }

            return propertyValueAsComparable.CompareTo(from) < 0 || propertyValueAsComparable.CompareTo(to) > 0
                ? Fail($"{GetPropertyName()} must be between {from} and {to} (inclusive)")
                : null;
        });

    public IRuleBuilder<TArgument, TProperty> ExclusiveBetween(TProperty from, TProperty to)
        => AddRule(instance =>
        {
            var propertyValue = GetPropertyValue(instance);
            if (propertyValue is not IComparable<TProperty> propertyValueAsComparable)
            {
                return null;
            }

            return propertyValueAsComparable.CompareTo(from) <= 0 || propertyValueAsComparable.CompareTo(to) >= 0
                ? Fail($"{GetPropertyName()} must be between {from} and {to} (exclusive)")
                : null;
        });
}
