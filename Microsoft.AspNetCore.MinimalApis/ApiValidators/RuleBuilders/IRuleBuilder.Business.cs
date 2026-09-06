namespace Samples.MinimalApis.ApiValidators.RuleBuilders;

public partial interface IRuleBuilder<TArgument, TProperty>
    where TArgument : class
{
    IRuleBuilder<TArgument, TProperty> EmailAddress();
    IRuleBuilder<TArgument, TProperty> AbsoluteUrl();
    IRuleBuilder<TArgument, TProperty> RelativeUrl();
    IRuleBuilder<TArgument, TProperty> MustBeUrl();
    IRuleBuilder<TArgument, TProperty> CreditCard();
}
