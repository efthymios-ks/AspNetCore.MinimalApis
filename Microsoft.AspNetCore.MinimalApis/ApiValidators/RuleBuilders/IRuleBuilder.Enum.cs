namespace Samples.MinimalApis.ApiValidators.RuleBuilders;

public partial interface IRuleBuilder<TArgument, TProperty>
    where TArgument : class
{
    IRuleBuilder<TArgument, TProperty> IsInEnum();
    IRuleBuilder<TArgument, TProperty> IsInEnum<TEnum>()
        where TEnum : struct, Enum;
}
