namespace Microsoft.AspNetCore.MinimalApis.Utilities;

public sealed record CachedResponse(
    int StatusCode,
    string? ContentType,
    byte[] Body
);
