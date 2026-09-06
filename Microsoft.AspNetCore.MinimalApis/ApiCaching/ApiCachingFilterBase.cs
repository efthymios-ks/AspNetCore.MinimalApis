using Microsoft.AspNetCore.MinimalApis.EndpointFilterDelegateResults;
using Microsoft.AspNetCore.MinimalApis.Utilities;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Microsoft.AspNetCore.MinimalApis.ApiCaching;

internal abstract class ApiCachingFilterBase(ApiCachingOptionsBase options)
    : IEndpointFilter
{
    protected ApiCachingOptionsBase Options { get; } = options;

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        var httpContext = context.HttpContext;

        if (!IsCachingAllowed(httpContext.Request))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status405MethodNotAllowed,
                title: "Caching not allowed",
                detail: "Caching is only allowed for GET, HEAD, and OPTIONS requests."
            );
        }

        var cacheKey
            = ApiCachingOptionsBase.GetKeyPrefix(context)
            + Options.KeySuffixFactory(context);

        var (isHandled, cachedResult) = await TryHandleResponseFromCacheAsync(httpContext, cacheKey);
        if (isHandled)
        {
            return cachedResult;
        }

        var (result, responseToCache) = await RunAndCaptureResultAsync(context, next);
        await TryCacheResponseAsync(httpContext, cacheKey, responseToCache);
        return result;
    }

    public abstract Task<byte[]?> GetFromCacheAsync(
        string cacheKey,
        CancellationToken cancellationToken
    );

    public abstract Task AddToCacheAsync(
        string cacheKey,
        byte[] value,
        TimeSpan expiration,
        CancellationToken cancellationToken
    );

    private async Task<(bool isHandled, IResult? result)> TryHandleResponseFromCacheAsync(
        HttpContext httpContext,
        string cacheKey
    )
    {
        var httpResponse = httpContext.Response;
        if (httpResponse.HasStarted)
        {
            return (false, null);
        }

        var cachedResponseAsBytes = await GetFromCacheAsync(cacheKey, httpContext.RequestAborted);
        if (cachedResponseAsBytes is null)
        {
            return (false, null);
        }

        var cachedResponse = TryDeserialize(cachedResponseAsBytes);
        if (cachedResponse is null)
        {
            // Corrupt/incompatible entry — recompute.
            return (false, null);
        }

        httpResponse.StatusCode = cachedResponse.StatusCode;
        return (true, Results.Bytes(cachedResponse.Body, contentType: cachedResponse.ContentType));
    }

    private static CachedResponse? TryDeserialize(byte[] cachedResponseAsBytes)
    {
        try
        {
            return JsonSerializer.Deserialize<CachedResponse>(cachedResponseAsBytes);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static async Task<(object? result, CachedResponse? responseToCache)> RunAndCaptureResultAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        var wrappedResult = await context.RunAsync(next);
        if (wrappedResult is null)
        {
            return (null, null);
        }

        if (!IsCachingAllowed(wrappedResult.StatusCode))
        {
            return (wrappedResult.OriginalResult, null);
        }

        var responseToCache = new CachedResponse(
            StatusCode: wrappedResult.StatusCode,
            ContentType: wrappedResult.ContentType,
            Body: await wrappedResult.Value.ToBytesAsync(wrappedResult.ContentType, context.HttpContext.GetJsonSerializerOptions())
        );

        return (wrappedResult.OriginalResult, responseToCache);
    }

    private async Task TryCacheResponseAsync(
        HttpContext httpContext,
        string cacheKey,
        CachedResponse? responseToCache
    )
    {
        if (responseToCache is null)
        {
            return;
        }

        var responseToCacheAsBytes = JsonSerializer.SerializeToUtf8Bytes(responseToCache);
        await AddToCacheAsync(
            cacheKey,
            responseToCacheAsBytes,
            Options.CacheDuration,
            httpContext.RequestAborted
        );
    }

    private static bool IsCachingAllowed(HttpRequest httpRequest)
        => httpRequest.Method == HttpMethods.Get
        || httpRequest.Method == HttpMethods.Head
        || httpRequest.Method == HttpMethods.Options;

    private static bool IsCachingAllowed(int statusCode)
        => statusCode is StatusCodes.Status200OK;
}
