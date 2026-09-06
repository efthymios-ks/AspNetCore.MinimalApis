using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.MinimalApis.ApiValidators;

internal delegate IEnumerable<ValidationResult> ApiValidateDelegate(object argument);
