using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace Samples.MinimalApis.ApiValidators.RuleBuilders;

internal sealed partial class RuleBuilder<TArgument, TProperty> : IRuleBuilder<TArgument, TProperty>
    where TArgument : class
{
    private readonly Expression<Func<TArgument, TProperty>> _propertySelector;
    private readonly Func<TArgument, TProperty> _compiledSelector;
    private readonly List<Func<TArgument, ValidationResult?>> _rules;
    private readonly List<Func<TArgument, ValidationResult?>> _ownRules = [];

    public RuleBuilder(
        Expression<Func<TArgument, TProperty>> propertySelector,
        List<Func<TArgument, ValidationResult?>> rules
    )
    {
        ArgumentNullException.ThrowIfNull(propertySelector);
        ArgumentNullException.ThrowIfNull(rules);

        _propertySelector = propertySelector;
        _compiledSelector = propertySelector.Compile();
        _rules = rules;
        PropertyPath = ResolvePropertyName(propertySelector);
    }

    public string PropertyPath { get; }

    public TProperty GetPropertyValue(TArgument instance)
        => _compiledSelector(instance);

    public IRuleBuilder<TArgument, TProperty> AddRawRule(Func<TArgument, ValidationResult?> rule)
        => AddRule(rule);

    public IRuleBuilder<TArgument, TProperty> NotNull()
        => AddRule(instance => GetPropertyValue(instance) is null
            ? Fail($"{GetPropertyName()} is required")
            : null);

    public IRuleBuilder<TArgument, TProperty> Null()
        => AddRule(instance => GetPropertyValue(instance) is not null
            ? Fail($"{GetPropertyName()} must be null")
            : null);

    public IRuleBuilder<TArgument, TProperty> Must(Func<TProperty, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return AddRule(instance => !predicate(GetPropertyValue(instance))
            ? Fail($"{GetPropertyName()} is invalid")
            : null);
    }

    public IRuleBuilder<TArgument, TProperty> Must(Func<TArgument, TProperty, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return AddRule(instance => !predicate(instance, GetPropertyValue(instance))
            ? Fail($"{GetPropertyName()} is invalid")
            : null);
    }

    private RuleBuilder<TArgument, TProperty> AddRule(Func<TArgument, ValidationResult?> rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        _rules.Add(rule);
        _ownRules.Add(rule);
        return this;
    }

    private ValidationResult Fail(string message)
        => new(message, [GetPropertyName()]);

    private string GetPropertyName()
        => PropertyPath;

    private static string ResolvePropertyName(Expression<Func<TArgument, TProperty>> propertySelector)
    {
        if (propertySelector.Body is not MemberExpression memberExpression)
        {
            return string.Empty;
        }

        var propertyPath = new List<string>();
        var current = memberExpression;
        while (current is not null)
        {
            propertyPath.Add(current.Member.Name);
            current = current.Expression as MemberExpression;
        }

        propertyPath.Reverse();
        return string.Join(".", propertyPath);
    }
}
