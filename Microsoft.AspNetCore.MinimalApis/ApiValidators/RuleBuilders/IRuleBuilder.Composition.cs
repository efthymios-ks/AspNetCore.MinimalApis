using Microsoft.AspNetCore.MinimalApis.ApiValidators.RuleBuilders;

namespace Samples.MinimalApis.ApiValidators.RuleBuilders;

public partial interface IRuleBuilder<TArgument, TProperty>
    where TArgument : class
{
    IRuleBuilder<TArgument, TProperty> ForEach<TElement>(Action<InlineValidator<TElement>> elementRules)
        where TElement : class;
}
