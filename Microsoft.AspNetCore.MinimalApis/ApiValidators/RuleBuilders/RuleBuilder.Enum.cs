namespace Samples.MinimalApis.ApiValidators.RuleBuilders;

internal sealed partial class RuleBuilder<TArgument, TProperty>
{
    public IRuleBuilder<TArgument, TProperty> IsInEnum()
        => AddRule(instance =>
        {
            var propertyValue = GetPropertyValue(instance);
            if (propertyValue is not Enum propertyValueAsEnum)
            {
                return null;
            }

            return !Enum.IsDefined(propertyValueAsEnum.GetType(), propertyValueAsEnum)
                ? Fail($"{GetPropertyName()} has an invalid value")
                : null;
        });

    public IRuleBuilder<TArgument, TProperty> IsInEnum<TEnum>()
        where TEnum : struct, Enum
        => AddRule(instance =>
        {
            var propertyValue = GetPropertyValue(instance);
            if (propertyValue is null)
            {
                return null;
            }

            var enumType = typeof(TEnum);
            return !Enum.IsDefined(enumType, propertyValue)
                ? Fail($"{GetPropertyName()} must be a valid {enumType.Name} value")
                : null;
        });
}
