namespace Samples.MinimalApis.ApiValidators.RuleBuilders;

public partial interface IRuleBuilder<TArgument, TProperty>
    where TArgument : class
{
    IRuleBuilder<TArgument, TProperty> NotEmpty();
    IRuleBuilder<TArgument, TProperty> Empty();
    IRuleBuilder<TArgument, TProperty> Length(int exactLength);
    IRuleBuilder<TArgument, TProperty> Length(int minLength, int maxLength);
    IRuleBuilder<TArgument, TProperty> MinimumLength(int minLength);
    IRuleBuilder<TArgument, TProperty> MaximumLength(int maxLength);
}
