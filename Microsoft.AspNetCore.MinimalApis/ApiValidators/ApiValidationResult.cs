using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.MinimalApis.ApiValidators;

/// <summary>
/// A <see cref="ValidationResult"/> that additionally carries an error code.
/// The code is stored for callers that inspect results directly; the HTTP
/// validation-problem response remains message-only.
/// </summary>
public sealed class ApiValidationResult(
    string? errorMessage,
    IEnumerable<string>
    memberNames,
    string errorCode
    ) : ValidationResult(errorMessage, memberNames)
{
    public string ErrorCode { get; } = errorCode;
}
