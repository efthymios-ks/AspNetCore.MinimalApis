namespace Microsoft.AspNetCore.MinimalApis.EndpointFilterDelegateResults;

internal sealed class EndpointFilterDelegateResult
{
    public required object? OriginalResult { get; init; }
    public required int StatusCode { get; init; }
    public required object? Value { get; init; }
    public required string ContentType { get; init; }
}
