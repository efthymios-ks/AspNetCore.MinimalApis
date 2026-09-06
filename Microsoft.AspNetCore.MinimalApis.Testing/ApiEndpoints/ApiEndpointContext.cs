using Asp.Versioning;
using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;
using Microsoft.AspNetCore.MinimalApis.ApiVersions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.AspNetCore.MinimalApis.Testing.ApiEndpoints;

public sealed class ApiEndpointContext : IAsyncDisposable
{
    private bool _isDisposed;
    private readonly IReadOnlyList<IAsyncDisposable> _disposables;
    private readonly Func<Task<object?>> _invokeCore;

    internal ApiEndpointContext(
        DefaultHttpContext httpContext,
        ApiMetadata metadata,
        Func<Task<object?>> invokeCore,
        IEnumerable<IAsyncDisposable> disposables
    )
    {
        HttpContext = httpContext;
        Metadata = metadata;
        _invokeCore = invokeCore;
        _disposables = [.. disposables];
    }

    public HttpContext HttpContext { get; }
    public ApiMetadata Metadata { get; }

    public Task<object?> InvokeAsync()
        => _invokeCore();

    public ApiEndpointContext WithRouteValue(string key, object? value)
        => WithRouteValues(new Dictionary<string, object?>
        {
            [key] = value
        });

    public ApiEndpointContext WithRouteValues(Dictionary<string, object?> values)
    {
        foreach (var (routeKey, routeValue) in values)
        {
            HttpContext.Request.RouteValues[routeKey] = routeValue;
        }

        return this;
    }

    public ApiEndpointContext WithQueryParam(string key, string value)
        => WithQueryParams(new Dictionary<string, string>
        {
            [key] = value
        });

    public ApiEndpointContext WithQueryParams(Dictionary<string, string> values)
    {
        var query = HttpContext
            .Request
            .Query
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        foreach (var (queryKey, queryValue) in values)
        {
            query[queryKey] = queryValue;
        }

        HttpContext.Request.Query = new QueryCollection(query);
        return this;
    }

    public ApiEndpointContext WithHeader(string key, string value)
        => WithHeaders(new Dictionary<string, string>
        {
            [key] = value
        });

    public ApiEndpointContext WithHeaders(Dictionary<string, string> values)
    {
        foreach (var (headerKey, headerValue) in values)
        {
            HttpContext.Request.Headers[headerKey] = headerValue;
        }

        return this;
    }

    public ApiEndpointContext WithJsonBody<TElement>(TElement body, JsonSerializerOptions? options = null)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(body, options);
        var stream = new MemoryStream(json);

        HttpContext.Request.Body = stream;
        HttpContext.Request.ContentType = MediaTypeNames.Application.Json;
        HttpContext.Request.ContentLength = json.Length;
        HttpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(canHaveBody: true));

        return this;
    }

    public ApiEndpointContext WithXmlBody<TElement>(TElement body, XmlWriterSettings? settings = null)
    {
        var writerSettings = settings ?? new XmlWriterSettings
        {
            Encoding = Encoding.UTF8
        };

        var memoryStream = new MemoryStream();
        var serializer = new XmlSerializer(typeof(TElement));
        using (var writer = XmlWriter.Create(memoryStream, writerSettings))
        {
            serializer.Serialize(writer, body);
        }

        memoryStream.Position = 0;
        HttpContext.Request.Body = memoryStream;
        HttpContext.Request.ContentType = MediaTypeNames.Application.Xml;
        HttpContext.Request.ContentLength = memoryStream.Length;
        HttpContext.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBodyDetectionFeature(canHaveBody: true));
        return this;
    }

    public ApiEndpointContext WithFormField(string key, string value)
        => WithFormFields(new Dictionary<string, string>
        {
            [key] = value
        });

    public ApiEndpointContext WithFormFields(Dictionary<string, string> values)
    {
        if (!HttpContext.Request.HasFormContentType)
        {
            HttpContext.Request.ContentType = MediaTypeNames.Application.FormUrlEncoded;
        }

        var formCollection = HttpContext.Request.Form;
        var formFields = formCollection?
            .ToDictionary(pair => pair.Key, pair => pair.Value)
            ?? [];
        foreach (var (formKey, formValue) in values)
        {
            formFields[formKey] = formValue;
        }

        HttpContext.Request.Form = new FormCollection(formFields, formCollection?.Files);
        return this;
    }

    public ApiEndpointContext WithFormFile(
        string fieldName,
        byte[] content,
        string fileName,
        string contentType = MediaTypeNames.Application.Octet
    )
    {
        var formFeature = HttpContext.Features.Get<IFormFeature>();
        var formCollection = formFeature?.Form;
        var formFields = formCollection?
            .ToDictionary(pair => pair.Key, pair => pair.Value)
            ?? [];

        var files = new FormFileCollection();
        if (formCollection?.Files is { } formFiles)
        {
            files.AddRange(formFiles);
        }

        files.Add(new FormFile(new MemoryStream(content), 0, content.Length, fieldName, fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        });

        HttpContext.Request.ContentType = "multipart/form-data; boundary=----FormBoundary";
        HttpContext.Request.Form = new FormCollection(formFields, files);
        return this;
    }

    public ApiEndpointContext WithCancellationToken(CancellationToken cancellationToken)
    {
        HttpContext.RequestAborted = cancellationToken;
        return this;
    }

    public async Task<string> ReadJsonBodyAsync(
        CancellationToken cancellationToken = default
    )
    {
        var body = HttpContext.Response.Body;
        body.Position = 0;
        using var reader = new StreamReader(body, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    public async Task<TElement?> ReadJsonBodyAsync<TElement>(
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        var body = HttpContext.Response.Body;
        body.Position = 0;
        options ??= HttpContext.RequestServices.GetService<IOptions<JsonOptions>>()?.Value.SerializerOptions;
        return await JsonSerializer.DeserializeAsync<TElement>(body, options, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        foreach (var disposable in _disposables)
        {
            await disposable.DisposeAsync();
        }
    }

    public static ApiEndpointContext Create<TEndpoint>(
        Action<IServiceCollection>? configure = null
    ) where TEndpoint : ApiEndpoint, new()
    {
        var serviceCollection = new ServiceCollection();
        configure?.Invoke(serviceCollection);
        serviceCollection.AddLogging();
        serviceCollection.AddRouting();

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var routeBuilder = new InMemoryEndpointRouteBuilder(serviceProvider);
        var routeHandlerBuilder = new TEndpoint().MapEndpoint(routeBuilder);

        // Must be registered BEFORE Endpoints is accessed — that access triggers the lazy RequestDelegate build
        object? captured = null;
        routeHandlerBuilder.AddEndpointFilter(async (context, next) =>
        {
            var result = await next(context);
            captured = result;
            return result;
        });

        var routeEndpoint = routeBuilder
            .DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .First();

        var metadata = new ApiMetadata(
            route: () => routeEndpoint.RoutePattern.RawText ?? string.Empty,
            httpMethod: () => GetHttpMethod(routeEndpoint),
            metadata: () => [.. routeEndpoint.Metadata],
            requiresAuthorization: () => GetRequiresAuthorization(routeEndpoint),
            version: () => GetVersionInfo(typeof(TEndpoint)),
            classAttributes: () => GetClassAttributes(typeof(TEndpoint))
        );

        var responseBody = new MemoryStream();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        httpContext.Request.Method = GetHttpMethod(routeEndpoint);
        httpContext.Request.Path = GetRequestPath(routeEndpoint);
        httpContext.Response.Body = responseBody;

        async Task<object?> InvokeCore()
        {
            captured = null;
            responseBody.SetLength(0);
            httpContext.Request.Body.Position = 0;

            await routeEndpoint.RequestDelegate!(httpContext);
            responseBody.Position = 0;
            return captured;
        }

        return new(httpContext, metadata, InvokeCore, [responseBody, serviceProvider]);
    }

    private static string GetHttpMethod(RouteEndpoint routeEndpoint)
    {
        var methods = routeEndpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods;
        return methods is { Count: > 0 } ? methods[0] : string.Empty;
    }

    private static PathString GetRequestPath(RouteEndpoint routeEndpoint)
    {
        var route = routeEndpoint.RoutePattern.RawText;
        return string.IsNullOrEmpty(route)
            ? PathString.Empty
            : new PathString(route.StartsWith('/') ? route : "/" + route);
    }

    private static bool GetRequiresAuthorization(RouteEndpoint routeEndpoint)
        => routeEndpoint.Metadata.GetMetadata<IAuthorizeData>() is not null
        && routeEndpoint.Metadata.GetMetadata<IAllowAnonymous>() is null;

    private static ApiVersionInfo? GetVersionInfo(Type endpointType)
    {
        var versionGroupAttribute = endpointType.GetCustomAttribute<ApiVersionGroupAttribute>(inherit: false);
        var versionAttribute = endpointType.GetCustomAttribute<ApiVersionAttribute>(inherit: false);
        if (versionGroupAttribute is null || versionAttribute is null)
        {
            return null;
        }

        return new()
        {
            Group = versionGroupAttribute.Group,
            Version = (int)versionAttribute.Versions[0].MajorVersion!,
            IsDeprecated = versionAttribute.Deprecated
        };
    }

    private static Attribute[] GetClassAttributes(Type endpointType)
        => Attribute.GetCustomAttributes(endpointType, inherit: false);
}
