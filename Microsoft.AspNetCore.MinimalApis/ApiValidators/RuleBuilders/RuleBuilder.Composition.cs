using Microsoft.AspNetCore.MinimalApis.ApiValidators.RuleBuilders;
using System.Collections;

namespace Samples.MinimalApis.ApiValidators.RuleBuilders;

internal sealed partial class RuleBuilder<TArgument, TProperty>
{
    public IRuleBuilder<TArgument, TProperty> ForEach<TElement>(Action<InlineValidator<TElement>> elementRules)
        where TElement : class
    {
        ArgumentNullException.ThrowIfNull(elementRules);

        var inline = new InlineValidator<TElement>();
        elementRules(inline);

        return AddRule(instance =>
        {
            var propertyValue = GetPropertyValue(instance);
            if (propertyValue is not IEnumerable enumerable)
            {
                return null;
            }

            var index = 0;
            foreach (var item in enumerable)
            {
                if (item is TElement element)
                {
                    var failure = inline.Validate(element).FirstOrDefault();
                    if (failure is not null)
                    {
                        return ValidationResultPrefixer.Prefix($"{GetPropertyName()}[{index}]", failure);
                    }
                }

                index++;
            }

            return null;
        });
    }
}
