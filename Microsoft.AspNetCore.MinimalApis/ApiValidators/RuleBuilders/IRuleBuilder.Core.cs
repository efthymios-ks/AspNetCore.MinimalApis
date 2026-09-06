using System.ComponentModel.DataAnnotations;

namespace Samples.MinimalApis.ApiValidators.RuleBuilders;

public partial interface IRuleBuilder<TArgument, TProperty>
    where TArgument : class
{
    string PropertyPath { get; }
    TProperty GetPropertyValue(TArgument instance);
    IRuleBuilder<TArgument, TProperty> AddRawRule(Func<TArgument, ValidationResult?> rule);

    IRuleBuilder<TArgument, TProperty> NotNull();
    IRuleBuilder<TArgument, TProperty> Null();
    IRuleBuilder<TArgument, TProperty> Must(Func<TProperty, bool> predicate);
    IRuleBuilder<TArgument, TProperty> Must(Func<TArgument, TProperty, bool> predicate);
}
