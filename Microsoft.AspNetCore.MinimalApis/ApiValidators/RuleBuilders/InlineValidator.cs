using Samples.MinimalApis.ApiValidators.RuleBuilders;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace Microsoft.AspNetCore.MinimalApis.ApiValidators.RuleBuilders;

/// <summary>
/// A validator whose rules are configured inline, used by <c>ChildRules</c> and
/// <c>ForEach</c> to validate nested objects and collection elements.
/// </summary>
public sealed class InlineValidator<TElement> : ApiValidator<TElement>
    where TElement : class
{
    public new IRuleBuilder<TElement, TProperty> RuleFor<TProperty>(Expression<Func<TElement, TProperty>> propertySelector)
        => base.RuleFor(propertySelector);
}

internal static class ValidationResultPrefixer
{
    internal static ValidationResult Prefix(string parentPath, ValidationResult failure)
    {
        var members = failure.MemberNames
            .Select(member => string.IsNullOrEmpty(member) ? parentPath : $"{parentPath}.{member}")
            .ToArray();

        if (members.Length is 0)
        {
            members = [parentPath];
        }

        return failure is ApiValidationResult apiValidationResult
            ? new ApiValidationResult(failure.ErrorMessage, members, apiValidationResult.ErrorCode)
            : new ValidationResult(failure.ErrorMessage, members);
    }
}
