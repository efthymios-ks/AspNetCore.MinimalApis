using Microsoft.AspNetCore.MinimalApis.EndpointFilterDelegateResults;
using Microsoft.AspNetCore.MinimalApis.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.MinimalApis.ApiIdempotency;

internal sealed class ApiIdempotencyFilter(
    IApiIdempotencyStore store,
    ILogger<ApiIdempotencyFilter> logger,
    IOptions<ApiIdempotencyOptions> options
) : IEndpointFilter
{
    private readonly IApiIdempotencyStore _store = store;
    private readonly ILogger _logger = logger;
    private readonly ApiIdempotencyOptions _options = options.Value;

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        var httpContext = context.HttpContext;

        if (!IsIdempotentMethod(httpContext.Request))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status405MethodNotAllowed,
                title: "Idempotency not allowed",
                detail: "Idempotency is only allowed for POST requests."
            );
        }

        var keySuffix = _options.KeySuffixFactory(context);
        if (string.IsNullOrWhiteSpace(keySuffix))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Idempotency key required",
                detail: "A non-empty idempotency key is required for this request."
            );
        }

        var idempotencyKey
            = ApiIdempotencyOptions.GetKeyPrefix(context)
            + keySuffix;
        var cancellationToken = httpContext.RequestAborted;

        var entry = await _store.TryReserveAsync(
            idempotencyKey,
            _options.ProcessingTimeout,
            cancellationToken
        );

        switch (entry.Status)
        {
            case IdempotencyStatus.Completed:
                var stored = entry.Response!;
                httpContext.Response.StatusCode = stored.StatusCode;
                return Results.Bytes(stored.Body, contentType: stored.ContentType);

            case IdempotencyStatus.Pending:
                _logger.LogWarning(
                    "Idempotent request already in progress for key: {IdempotencyKey}",
                    idempotencyKey
                );

                return Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Request in progress",
                    detail: "A request with the same idempotency key is already being processed."
                );

            default:
                return await RunAndCaptureAsync(context, next, idempotencyKey, cancellationToken);
        }
    }

    private async ValueTask<object?> RunAndCaptureAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next,
        string idempotencyKey,
        CancellationToken cancellationToken
    )
    {
        EndpointFilterDelegateResult? wrappedResult;
        try
        {
            wrappedResult = await context.RunAsync(next);
        }
        catch
        {
            // Handler threw — release so the client can retry.
            await SafeReleaseAsync(idempotencyKey);
            throw;
        }

        // Store only successful responses; release the rest so the client can retry.
        if (wrappedResult is null || !IsSuccess(wrappedResult.StatusCode))
        {
            await SafeReleaseAsync(idempotencyKey);
            return wrappedResult?.OriginalResult;
        }

        var response = new CachedResponse(
            StatusCode: wrappedResult.StatusCode,
            ContentType: wrappedResult.ContentType,
            Body: await wrappedResult.Value.ToBytesAsync(wrappedResult.ContentType, context.HttpContext.GetJsonSerializerOptions())
        );

        await _store.CompleteAsync(
            idempotencyKey,
            response,
            _options.CacheDuration,
            cancellationToken
        );

        return wrappedResult.OriginalResult;
    }

    private async Task SafeReleaseAsync(string idempotencyKey)
    {
        try
        {
            // Cleanup must not cancel or throw over the real outcome.
            await _store.ReleaseAsync(idempotencyKey, CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to release idempotency reservation for key: {IdempotencyKey}",
                idempotencyKey
            );
        }
    }

    private static bool IsSuccess(int statusCode)
        => statusCode is >= StatusCodes.Status200OK and < StatusCodes.Status300MultipleChoices;

    private static bool IsIdempotentMethod(HttpRequest httpRequest)
        => StringEquals(httpRequest.Method, HttpMethods.Post);

    private static bool StringEquals(string? left, string? right)
        => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
