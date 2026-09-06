namespace Samples.MinimalApis.ApiValidators.RuleBuilders;

public partial interface IRuleBuilder<TArgument, TProperty>
    where TArgument : class
{
    IRuleBuilder<TArgument, TProperty> Zero();
    IRuleBuilder<TArgument, TProperty> NotZero();
    IRuleBuilder<TArgument, TProperty> Positive();
    IRuleBuilder<TArgument, TProperty> PositiveOrZero();
    IRuleBuilder<TArgument, TProperty> Negative();
    IRuleBuilder<TArgument, TProperty> NegativeOrZero();
}
