using Samples.MinimalApis.ApiValidators.RuleBuilders;

namespace Microsoft.AspNetCore.MinimalApis.ApiValidators.RuleBuilders;

public static class RuleBuilderCompositionExtensions
{
    /// <summary>
    /// Validates the property with another validator, prefixing member names with the property path.
    /// </summary>
    public static IRuleBuilder<TArgument, TProperty> SetValidator<TArgument, TProperty>(
        this IRuleBuilder<TArgument, TProperty> builder,
        ApiValidator<TProperty> validator
    )
        where TArgument : class
        where TProperty : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(validator);

        return builder.AddRawRule(instance =>
        {
            var value = builder.GetPropertyValue(instance);
            if (value is null)
            {
                return null;
            }

            var failure = validator.Validate(value).FirstOrDefault();
            return failure is null ? null : ValidationResultPrefixer.Prefix(builder.PropertyPath, failure);
        });
    }

    /// <summary>
    /// Validates the property with inline rules, prefixing member names with the property path.
    /// </summary>
    public static IRuleBuilder<TArgument, TProperty> ChildRules<TArgument, TProperty>(
        this IRuleBuilder<TArgument, TProperty> builder,
        Action<InlineValidator<TProperty>> rules
    )
        where TArgument : class
        where TProperty : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(rules);

        var inline = new InlineValidator<TProperty>();
        rules(inline);
        return builder.SetValidator(inline);
    }
}
