using Microsoft.AspNetCore.Http;
using System.Net.Mime;

namespace Microsoft.AspNetCore.MinimalApis.EndpointFilterDelegateResults;

internal static class EndpointFilterDelegateResultExtensions
{
    public static async Task<EndpointFilterDelegateResult?> RunAsync(
        this EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        var result = await next(context);
        if (result is null)
        {
            return null;
        }

        if (result is not IResult)
        {
            throw new InvalidOperationException(
                $"API caching and idempotency capture the response from an {nameof(IResult)} "
                + $"(e.g. Results.Ok(...)). The endpoint returned '{result.GetType()}', which has no "
                + $"status code or content type to capture — return an {nameof(IResult)} instead."
            );
        }

        var unwrappedResult = UnwrapNestedResult(result);
        var statusCode = GetStatusCode(unwrappedResult);
        var value = GetValue(unwrappedResult);
        var contentType = GetContentType(unwrappedResult);
        return new()
        {
            OriginalResult = result,
            StatusCode = statusCode,
            Value = value,
            ContentType = contentType
        };
    }

    private static int GetStatusCode(object result)
        => result is IStatusCodeHttpResult statusCodeResult
        && statusCodeResult.StatusCode is not null
            ? statusCodeResult.StatusCode.Value
            : StatusCodes.Status200OK;

    private static object? GetValue(object result)
        => result is IValueHttpResult valueHttpResult
        && valueHttpResult.Value is not null
            ? valueHttpResult.Value
            : result;

    private static string GetContentType(object result)
        => result is IContentTypeHttpResult contentTypeResult
        && contentTypeResult.ContentType is not null
            ? contentTypeResult.ContentType
            : MediaTypeNames.Application.Json;

    private static object UnwrapNestedResult(object result)
        => result is INestedHttpResult nestedResult
        && nestedResult.Result is not null
            ? UnwrapNestedResult(nestedResult.Result)
            : result;
}
