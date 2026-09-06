using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.MinimalApis.Testing.ApiEndpoints;

internal sealed class RequestBodyDetectionFeature(bool canHaveBody)
    : IHttpRequestBodyDetectionFeature
{
    public bool CanHaveBody { get; } = canHaveBody;
}
