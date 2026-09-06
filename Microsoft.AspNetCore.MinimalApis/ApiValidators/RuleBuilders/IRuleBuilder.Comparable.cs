namespace Samples.MinimalApis.ApiValidators.RuleBuilders;

public partial interface IRuleBuilder<TArgument, TProperty>
    where TArgument : class
{
    IRuleBuilder<TArgument, TProperty> GreaterThan(TProperty valueToCompare);
    IRuleBuilder<TArgument, TProperty> GreaterThanOrEqualTo(TProperty valueToCompare);
    IRuleBuilder<TArgument, TProperty> LessThan(TProperty valueToCompare);
    IRuleBuilder<TArgument, TProperty> LessThanOrEqualTo(TProperty valueToCompare);
    IRuleBuilder<TArgument, TProperty> Equal(TProperty valueToCompare);
    IRuleBuilder<TArgument, TProperty> NotEqual(TProperty valueToCompare);
    IRuleBuilder<TArgument, TProperty> InclusiveBetween(TProperty from, TProperty to);
    IRuleBuilder<TArgument, TProperty> ExclusiveBetween(TProperty from, TProperty to);
}
