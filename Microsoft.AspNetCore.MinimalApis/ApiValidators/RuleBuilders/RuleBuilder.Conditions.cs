using Microsoft.AspNetCore.MinimalApis.ApiValidators;
using System.ComponentModel.DataAnnotations;

namespace Samples.MinimalApis.ApiValidators.RuleBuilders;

internal sealed partial class RuleBuilder<TArgument, TProperty>
{
    public IRuleBuilder<TArgument, TProperty> WithMessage(string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);

        ReplaceLast(result => new ValidationResult(message, result.MemberNames));
        return this;
    }

    public IRuleBuilder<TArgument, TProperty> WithName(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        ReplaceLast(result => new ValidationResult(result.ErrorMessage, [name]));
        return this;
    }

    public IRuleBuilder<TArgument, TProperty> WithErrorCode(string errorCode)
    {
        ArgumentException.ThrowIfNullOrEmpty(errorCode);

        ReplaceLast(result => new ApiValidationResult(result.ErrorMessage, result.MemberNames, errorCode));
        return this;
    }

    public IRuleBuilder<TArgument, TProperty> When(Func<TArgument, bool> condition)
    {
        ApplyCondition(condition, negate: false);
        return this;
    }

    public IRuleBuilder<TArgument, TProperty> Unless(Func<TArgument, bool> condition)
    {
        ApplyCondition(condition, negate: true);
        return this;
    }

    private void ReplaceLast(Func<ValidationResult, ValidationResult> transform)
    {
        if (_ownRules.Count is 0)
        {
            return;
        }

        var last = _ownRules[^1];

        ValidationResult? Wrapped(TArgument instance)
        {
            var result = last(instance);
            return result is null ? null : transform(result);
        }

        var index = _rules.LastIndexOf(last);
        if (index >= 0)
        {
            _rules[index] = Wrapped;
        }

        _ownRules[^1] = Wrapped;
    }

    private void ApplyCondition(Func<TArgument, bool> condition, bool negate)
    {
        ArgumentNullException.ThrowIfNull(condition);

        for (var i = 0; i < _ownRules.Count; i++)
        {
            var rule = _ownRules[i];

            ValidationResult? Wrapped(TArgument instance)
            {
                var conditionMet = condition(instance);
                var shouldRun = negate ? !conditionMet : conditionMet;
                return shouldRun ? rule(instance) : null;
            }

            var index = _rules.LastIndexOf(rule);
            if (index >= 0)
            {
                _rules[index] = Wrapped;
            }

            _ownRules[i] = Wrapped;
        }
    }
}
