using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;

namespace Microsoft.AspNetCore.MinimalApis.Utilities;

internal static class ContentUtils
{
    private static readonly ConcurrentDictionary<Type, XmlSerializer> _xmlSerializerCache = new();

    public static JsonSerializerOptions? GetJsonSerializerOptions(this HttpContext httpContext)
        => httpContext.RequestServices?
            .GetService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>()?
            .Value
            .SerializerOptions;

    public static async Task<byte[]> ToBytesAsync(
        this object? value,
        string? contentType,
        JsonSerializerOptions? jsonSerializerOptions = null
    )
    {
        if (value is null)
        {
            return [];
        }

        if (value is Stream valueAsStream)
        {
            using var memoryStream = new MemoryStream();
            await valueAsStream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        if (value is byte[] valueAsBytes)
        {
            return valueAsBytes;
        }

        if (value is string valueAsString)
        {
            return Encoding.UTF8.GetBytes(valueAsString);
        }

        if (IsContentTypeXml(contentType))
        {
            return ToXmlBytes(value);
        }

        return JsonSerializer.SerializeToUtf8Bytes(value, value.GetType(), jsonSerializerOptions);
    }

    private static bool IsContentTypeXml(string? contentType)
        => StringEquals(contentType, MediaTypeNames.Application.Xml)
        || StringEquals(contentType, MediaTypeNames.Application.XmlPatch)
        || StringEquals(contentType, MediaTypeNames.Application.ProblemXml)
        || StringEquals(contentType, MediaTypeNames.Text.Xml);

    private static byte[] ToXmlBytes(object value)
    {
        var serializer = _xmlSerializerCache.GetOrAdd(
            value.GetType(),
            type => new XmlSerializer(type)
        );

        using var stream = new MemoryStream();
        using var streamWriter = new StreamWriter(stream, Encoding.UTF8);
        serializer.Serialize(streamWriter, value);
        return stream.ToArray();
    }

    private static bool StringEquals(string? left, string? right)
        => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
