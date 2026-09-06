using System.Text.RegularExpressions;

namespace Samples.MinimalApis.ApiValidators.RuleBuilders;

public partial interface IRuleBuilder<TArgument, TProperty>
    where TArgument : class
{
    IRuleBuilder<TArgument, TProperty> Matches(
        string pattern,
        RegexOptions options
            = RegexOptions.Compiled
            | RegexOptions.CultureInvariant
            | RegexOptions.IgnoreCase
            | RegexOptions.Multiline
    );

    IRuleBuilder<TArgument, TProperty> Contains(
        string value,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase
    );

    IRuleBuilder<TArgument, TProperty> StartsWith(
        string value,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase
    );

    IRuleBuilder<TArgument, TProperty> EndsWith(
        string value,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase
    );
}
