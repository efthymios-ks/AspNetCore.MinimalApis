using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Samples.MinimalApis.ApiValidators.RuleBuilders;

internal sealed partial class RuleBuilder<TArgument, TProperty>
{
    private static readonly Lazy<EmailAddressAttribute> _emailAddressAttribute = new(() => new EmailAddressAttribute());
    private static readonly Lazy<CreditCardAttribute> _creditCardAttribute = new(() => new CreditCardAttribute());
    private static readonly Regex _absoluteUrl = AbsoluteUrlRegex();
    private static readonly Regex _relativeUrl = RelativeUrlRegex();

    public IRuleBuilder<TArgument, TProperty> EmailAddress()
        => AddRule(instance =>
        {
            var propertyValue = GetPropertyValue(instance);
            if (propertyValue is not string propertyValueAsString)
            {
                return null;
            }

            return !_emailAddressAttribute.Value.IsValid(propertyValueAsString)
                ? Fail($"{GetPropertyName()} is not a valid email address")
                : null;
        });

    public IRuleBuilder<TArgument, TProperty> AbsoluteUrl()
        => AddRule(instance =>
        {
            var propertyValue = GetPropertyValue(instance);
            if (propertyValue is not string propertyValueAsString)
            {
                return null;
            }

            return !_absoluteUrl.IsMatch(propertyValueAsString)
                ? Fail($"{GetPropertyName()} is not a valid absolute URL")
                : null;
        });

    public IRuleBuilder<TArgument, TProperty> RelativeUrl()
        => AddRule(instance =>
        {
            var propertyValue = GetPropertyValue(instance);
            if (propertyValue is not string propertyValueAsString)
            {
                return null;
            }

            return !_relativeUrl.IsMatch(propertyValueAsString)
                ? Fail($"{GetPropertyName()} is not a valid relative URL")
                : null;
        });

    public IRuleBuilder<TArgument, TProperty> MustBeUrl()
        => AddRule(instance =>
        {
            var propertyValue = GetPropertyValue(instance);
            if (propertyValue is not string propertyValueAsString)
            {
                return null;
            }

            return !Uri.IsWellFormedUriString(propertyValueAsString, UriKind.RelativeOrAbsolute)
                ? Fail($"{GetPropertyName()} must be a well-formed URI")
                : null;
        });

    public IRuleBuilder<TArgument, TProperty> CreditCard()
        => AddRule(instance =>
        {
            var propertyValue = GetPropertyValue(instance);
            if (propertyValue is not string propertyValueAsString)
            {
                return null;
            }

            return !_creditCardAttribute.Value.IsValid(propertyValueAsString)
                ? Fail($"{GetPropertyName()} is not a valid credit card number")
                : null;
        });

    [GeneratedRegex(@"^(https?|ftp)://[^\s/$.?#].[^\s]*$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AbsoluteUrlRegex();

    [GeneratedRegex(@"^(\/|\.\/|\.\.\/)[^\s]*$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RelativeUrlRegex();
}
