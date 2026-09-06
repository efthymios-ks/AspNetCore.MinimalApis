namespace Samples.MinimalApis.ApiValidators.RuleBuilders;

internal sealed partial class RuleBuilder<TArgument, TProperty>
{
    public IRuleBuilder<TArgument, TProperty> Zero()
        => CompareToZero(comparison => comparison != 0, "must be zero");

    public IRuleBuilder<TArgument, TProperty> NotZero()
        => CompareToZero(comparison => comparison == 0, "must not be zero");

    public IRuleBuilder<TArgument, TProperty> Positive()
        => CompareToZero(comparison => comparison <= 0, "must be positive");

    public IRuleBuilder<TArgument, TProperty> PositiveOrZero()
        => CompareToZero(comparison => comparison < 0, "must be positive or zero");

    public IRuleBuilder<TArgument, TProperty> Negative()
        => CompareToZero(comparison => comparison >= 0, "must be negative");

    public IRuleBuilder<TArgument, TProperty> NegativeOrZero()
        => CompareToZero(comparison => comparison > 0, "must be negative or zero");

    private RuleBuilder<TArgument, TProperty> CompareToZero(Func<int, bool> isFailure, string requirement)
        => AddRule(instance =>
        {
            var propertyValue = GetPropertyValue(instance);
            if (propertyValue is not IComparable propertyValueAsComparable)
            {
                return null;
            }

            var zero = Convert.ChangeType(0, propertyValue.GetType());
            return isFailure(propertyValueAsComparable.CompareTo(zero))
                ? Fail($"{GetPropertyName()} {requirement}")
                : null;
        });
}
