using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.MinimalApis.ApiResults;

public static class Extensions
{
    public static IResult Xml<TValue>(
        this IResultExtensions _,
        TValue value,
        int statusCode = StatusCodes.Status200OK
    ) => new XmlResult<TValue>(value, statusCode);
}
