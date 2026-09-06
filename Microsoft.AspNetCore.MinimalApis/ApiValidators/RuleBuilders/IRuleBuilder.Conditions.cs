namespace Samples.MinimalApis.ApiValidators.RuleBuilders;

public partial interface IRuleBuilder<TArgument, TProperty>
    where TArgument : class
{
    IRuleBuilder<TArgument, TProperty> WithMessage(string message);
    IRuleBuilder<TArgument, TProperty> WithName(string name);
    IRuleBuilder<TArgument, TProperty> WithErrorCode(string errorCode);
    IRuleBuilder<TArgument, TProperty> When(Func<TArgument, bool> condition);
    IRuleBuilder<TArgument, TProperty> Unless(Func<TArgument, bool> condition);
}
