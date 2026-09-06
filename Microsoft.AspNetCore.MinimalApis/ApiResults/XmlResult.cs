using Microsoft.AspNetCore.Http;
using System.Net.Mime;
using System.Xml.Serialization;

namespace Microsoft.AspNetCore.MinimalApis.ApiResults;

public sealed class XmlResult<TValue>(
    TValue value,
    int statusCode = StatusCodes.Status200OK
    ) : IValueHttpResult<TValue>,
        IValueHttpResult,
        IStatusCodeHttpResult,
        IContentTypeHttpResult,
        IResult
{
    private static readonly XmlSerializer _serializer = new(typeof(TValue));
    private readonly TValue _result = value;

    public TValue? Value { get; } = value;
    object? IValueHttpResult.Value { get; } = value;
    public int? StatusCode { get; } = statusCode;
    public string? ContentType { get; } = MediaTypeNames.Application.Xml;

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        using var stream = new MemoryStream();
        _serializer.Serialize(stream, _result);

        httpContext.Response.ContentType = ContentType;
        httpContext.Response.StatusCode = StatusCode!.Value;
        stream.Position = 0;
        await stream.CopyToAsync(httpContext.Response.Body);
    }
}
