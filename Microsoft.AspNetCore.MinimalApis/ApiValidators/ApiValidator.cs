using Samples.MinimalApis.ApiValidators.RuleBuilders;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.MinimalApis.ApiValidators;

public abstract class ApiValidator<TArgument>
    where TArgument : class
{
    private readonly List<Func<TArgument, ValidationResult?>> _rules = [];

    internal IEnumerable<ValidationResult> Validate(TArgument argument)
    {
        ArgumentNullException.ThrowIfNull(argument);

        return _rules
            .Select(rule => rule(argument))
            .Where(result => result is not null)!;
    }

    protected IRuleBuilder<TArgument, TProperty> RuleFor<TProperty>(
        Expression<Func<TArgument, TProperty>> propertySelector
    ) => new RuleBuilder<TArgument, TProperty>(propertySelector, _rules);

    /// <summary>
    /// Applies <paramref name="condition"/> to every rule declared inside <paramref name="rules"/>.
    /// Mirrors FluentValidation's block-form <c>When</c>.
    /// </summary>
    protected void When(Func<TArgument, bool> condition, Action rules)
        => ApplyConditionalRules(condition, rules, negate: false);

    /// <summary>
    /// Applies the negation of <paramref name="condition"/> to every rule declared inside
    /// <paramref name="rules"/>. Mirrors FluentValidation's block-form <c>Unless</c>.
    /// </summary>
    protected void Unless(Func<TArgument, bool> condition, Action rules)
        => ApplyConditionalRules(condition, rules, negate: true);

    private void ApplyConditionalRules(Func<TArgument, bool> condition, Action rules, bool negate)
    {
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(rules);

        var startIndex = _rules.Count;
        rules();

        for (var i = startIndex; i < _rules.Count; i++)
        {
            var rule = _rules[i];
            _rules[i] = instance =>
            {
                var conditionMet = condition(instance);
                return (negate ? !conditionMet : conditionMet) ? rule(instance) : null;
            };
        }
    }

    protected void Include(ApiValidator<TArgument> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);

        foreach (var rule in validator._rules)
        {
            _rules.Add(rule);
        }
    }
}
