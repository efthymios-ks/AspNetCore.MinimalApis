using Asp.Versioning;
using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;
using Microsoft.AspNetCore.MinimalApis.ApiVersions;
using Microsoft.AspNetCore.MinimalApis.Testing.ApiEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Net.Mime;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiEndpoints;

public sealed class ApiEndpointContextTests
{
    [Fact]
    public async Task Create_WhenCalled_ShouldExtractRouteAndHttpMethod()
    {
        // Arrange & Act
        await using var context = ApiEndpointContext.Create<TestEndpoint>();

        // Assert
        Assert.Equal("/test", context.Metadata.Route);
        Assert.Equal(HttpMethods.Get, context.Metadata.HttpMethod);
    }

    [Fact]
    public async Task Create_WhenEndpointIsGet_ShouldSetRequestMethodToGet()
    {
        // Arrange & Act
        await using var context = ApiEndpointContext.Create<TestEndpoint>();

        // Assert
        Assert.Equal(HttpMethods.Get, context.HttpContext.Request.Method);
    }

    [Fact]
    public async Task Create_WhenEndpointIsPost_ShouldSetRequestMethodToPost()
    {
        // Arrange & Act
        await using var context = ApiEndpointContext.Create<PostEndpoint>();

        // Assert
        Assert.Equal(HttpMethods.Post, context.HttpContext.Request.Method);
    }

    [Fact]
    public async Task Create_WhenCalled_ShouldSetRequestPathToRoute()
    {
        // Arrange & Act
        await using var context = ApiEndpointContext.Create<TestEndpoint>();

        // Assert
        Assert.Equal("/test", context.HttpContext.Request.Path.Value);
    }

    [Fact]
    public async Task Create_WhenCalled_ShouldProvideMemoryStreamResponseBody()
    {
        // Arrange & Act
        await using var context = ApiEndpointContext.Create<TestEndpoint>();

        // Assert
        Assert.IsType<MemoryStream>(context.HttpContext.Response.Body);
    }

    [Fact]
    public async Task Create_WhenCalled_ShouldPopulateMetadataCollection()
    {
        // Arrange & Act
        await using var context = ApiEndpointContext.Create<TestEndpoint>();

        // Assert
        Assert.NotEmpty(context.Metadata.Metadata);
        Assert.Contains(context.Metadata.Metadata, metadata
            => metadata is IHttpMethodMetadata
        );
    }

    [Fact]
    public async Task Create_WhenEndpointHasCustomMetadata_ShouldIncludeItInMetadataCollection()
    {
        // Arrange & Act
        await using var context = ApiEndpointContext.Create<TaggedEndpoint>();

        // Assert
        Assert.Contains(context.Metadata.Metadata, metadata
            => metadata is ITagsMetadata tags
            && tags.Tags.Contains("Products")
        );
    }

    [Fact]
    public async Task InvokeAsync_WhenCalled_ShouldReturnHandlerResult()
    {
        // Arrange
        await using var context = ApiEndpointContext.Create<TestEndpoint>();

        // Act
        var result = await context.InvokeAsync();

        // Assert
        var statusCodeResult = Assert.IsType<IStatusCodeHttpResult>(result, exactMatch: false);
        Assert.Equal(StatusCodes.Status200OK, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenCalled_ShouldWriteHandlerResultToResponseBody()
    {
        // Arrange
        await using var context = ApiEndpointContext.Create<TestEndpoint>();

        // Act
        await context.InvokeAsync();

        // Assert
        var responseBody = context.HttpContext.Response.Body;
        responseBody.Position = 0;
        var content = await new StreamReader(responseBody).ReadToEndAsync();
        Assert.Equal("\"hi\"", content);
    }

    [Fact]
    public async Task InvokeAsync_WhenCalledMultipleTimes_ShouldReturnFreshResult()
    {
        // Arrange
        await using var context = ApiEndpointContext.Create<TestEndpoint>();

        // Act
        var result1 = await context.InvokeAsync();
        var result2 = await context.InvokeAsync();

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotSame(result1, result2);
    }

    [Fact]
    public async Task InvokeAsync_WhenServiceIsRegistered_ShouldResolveFromDi()
    {
        // Arrange
        var greeter = new HelloGreeter();
        await using var context = ApiEndpointContext.Create<ServiceEndpoint>(services
            => services.AddSingleton<IGreeter>(greeter)
        );

        // Act
        var result = await context.InvokeAsync();

        // Assert
        var valueResult = Assert.IsType<IValueHttpResult>(result, exactMatch: false);
        Assert.Equal("hello", valueResult.Value);
        Assert.Equal(1, greeter.CallCount);
    }

    [Fact]
    public async Task InvokeAsync_WhenHandlerReturnsIResult_ShouldReturnIResult()
    {
        // Arrange
        await using var context = ApiEndpointContext.Create<TestEndpoint>();

        // Act
        var result = await context.InvokeAsync();

        // Assert
        Assert.IsType<IResult>(result, exactMatch: false);
    }

    [Fact]
    public async Task InvokeAsync_WhenHandlerReturnsPlainValue_ShouldReturnValue()
    {
        // Arrange
        await using var context = ApiEndpointContext.Create<PlainValueEndpoint>();

        // Act
        var result = await context.InvokeAsync();

        // Assert
        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task InvokeAsync_WhenHandlerReturnsNothing_ShouldReturnEmptyHttpResult()
    {
        // Arrange
        await using var context = ApiEndpointContext.Create<VoidEndpoint>();

        // Act
        var result = await context.InvokeAsync();

        // Assert
        Assert.Equal(EmptyHttpResult.Instance, result);
    }

    [Fact]
    public async Task Create_WhenRequireAuthorizationIsSet_ShouldSetRequiresAuthorization()
    {
        // Arrange & Act
        await using var context = ApiEndpointContext.Create<ProtectedEndpoint>();

        // Assert
        Assert.True(context.Metadata.RequiresAuthorization);
    }

    [Fact]
    public async Task Create_WhenAllowAnonymousIsSet_ShouldNotSetRequiresAuthorization()
    {
        // Arrange & Act
        await using var context = ApiEndpointContext.Create<AnonymousEndpoint>();

        // Assert
        Assert.False(context.Metadata.RequiresAuthorization);
    }

    [Fact]
    public async Task Create_WhenNoAuthMetadataIsPresent_ShouldNotSetRequiresAuthorization()
    {
        // Arrange & Act
        await using var context = ApiEndpointContext.Create<TestEndpoint>();

        // Assert
        Assert.False(context.Metadata.RequiresAuthorization);
    }

    [Fact]
    public async Task Create_WhenEndpointHasClassAttributes_ShouldPopulateClassAttributes()
    {
        // Arrange & Act
        await using var context = ApiEndpointContext.Create<VersionedEndpoint>();

        // Assert
        Assert.Contains(context.Metadata.ClassAttributes, attribute => attribute is ApiVersionGroupAttribute);
        Assert.Contains(context.Metadata.ClassAttributes, attribute => attribute is ApiVersionAttribute);
    }

    [Fact]
    public async Task Create_WhenEndpointIsVersioned_ShouldPopulateVersion()
    {
        // Arrange & Act
        await using var context = ApiEndpointContext.Create<VersionedEndpoint>();

        // Assert
        Assert.NotNull(context.Metadata.Version);
        Assert.Equal("versioned-endpoint", context.Metadata.Version.Group);
        Assert.Equal(2, context.Metadata.Version.Version);
        Assert.True(context.Metadata.Version.IsDeprecated);
    }

    [Fact]
    public async Task Create_WhenEndpointIsNotVersioned_ShouldHaveNullVersion()
    {
        // Arrange & Act
        await using var context = ApiEndpointContext.Create<TestEndpoint>();

        // Assert
        Assert.Null(context.Metadata.Version);
    }

    [Fact]
    public async Task DisposeAsync_WhenCalled_ShouldDisposeRegisteredServices()
    {
        // Arrange
        var context = ApiEndpointContext.Create<TestEndpoint>(services =>
            services.AddSingleton<DisposableService>());

        var service = context.HttpContext.RequestServices.GetRequiredService<DisposableService>();

        // Act
        await context.DisposeAsync();

        // Assert
        Assert.True(service.IsDisposed);
    }

    [Fact]
    public async Task WithRouteValue_WhenSet_ShouldBindToHandler()
    {
        // Arrange
        await using var context = ApiEndpointContext.Create<EchoRouteEndpoint>();

        // Act
        var result = await context.WithRouteValue("id", "42").InvokeAsync();

        // Assert
        var value = Assert.IsType<IValueHttpResult>(result, exactMatch: false);
        Assert.Equal("42", value.Value);
    }

    [Fact]
    public async Task WithRouteValues_WhenSetMultiple_ShouldApplyAllToRouteValueCollection()
    {
        // Arrange
        await using var context = ApiEndpointContext.Create<EchoRouteEndpoint>();

        // Act
        context.WithRouteValues(new Dictionary<string, object?> { ["id"] = "42", ["extra"] = "x" });

        // Assert
        Assert.Equal("42", context.HttpContext.Request.RouteValues["id"]);
        Assert.Equal("x", context.HttpContext.Request.RouteValues["extra"]);
    }

    [Fact]
    public async Task WithQueryParam_WhenSet_ShouldBindToHandler()
    {
        // Arrange
        await using var context = ApiEndpointContext.Create<EchoQueryEndpoint>();

        // Act
        var result = await context.WithQueryParam("name", "Alice").InvokeAsync();

        // Assert
        var value = Assert.IsType<IValueHttpResult>(result, exactMatch: false);
        Assert.Equal("Alice", value.Value);
    }

    [Fact]
    public async Task WithQueryParams_WhenSetMultiple_ShouldApplyAllToQueryCollection()
    {
        // Arrange
        await using var context = ApiEndpointContext.Create<EchoQueryEndpoint>();

        // Act
        context.WithQueryParams(new Dictionary<string, string> { ["name"] = "Alice", ["role"] = "admin" });

        // Assert
        Assert.Equal("Alice", context.HttpContext.Request.Query["name"]);
        Assert.Equal("admin", context.HttpContext.Request.Query["role"]);
    }

    [Fact]
    public async Task WithHeader_WhenSet_ShouldBindToHandler()
    {
        // Arrange
        await using var context = ApiEndpointContext.Create<EchoHeaderEndpoint>();

        // Act
        var result = await context.WithHeader("X-Name", "Alice").InvokeAsync();

        // Assert
        var value = Assert.IsType<IValueHttpResult>(result, exactMatch: false);
        Assert.Equal("Alice", value.Value);
    }

    [Fact]
    public async Task WithHeaders_WhenSetMultiple_ShouldApplyAllToHeaderCollection()
    {
        // Arrange
        await using var context = ApiEndpointContext.Create<EchoHeaderEndpoint>();

        // Act
        context.WithHeaders(new Dictionary<string, string> { ["X-Name"] = "Alice", ["X-Role"] = "admin" });

        // Assert
        Assert.Equal("Alice", context.HttpContext.Request.Headers["X-Name"]);
        Assert.Equal("admin", context.HttpContext.Request.Headers["X-Role"]);
    }

    [Fact]
    public async Task WithJsonBody_WhenSet_ShouldBindToHandler()
    {
        // Arrange
        await using var context = ApiEndpointContext.Create<EchoJsonBodyEndpoint>();

        // Act
        var result = await context.WithJsonBody(new Payload("Alice")).InvokeAsync();

        // Assert
        var value = Assert.IsType<IValueHttpResult>(result, exactMatch: false);
        Assert.Equal("Alice", value.Value);
    }

    [Fact]
    public async Task WithXmlBody_WhenSet_ShouldSetBodyAndContentType()
    {
        // Arrange
        await using var context = ApiEndpointContext.Create<EchoXmlBodyEndpoint>();

        // Act
        var result = await context.WithXmlBody("Alice").InvokeAsync();

        // Assert
        var value = Assert.IsType<IValueHttpResult>(result, exactMatch: false);
        Assert.Equal(MediaTypeNames.Application.Xml, context.HttpContext.Request.ContentType);
        Assert.Contains("Alice", value.Value?.ToString());
    }

    [Fact]
    public async Task WithFormField_WhenSet_ShouldBindToHandler()
    {
        // Arrange
        await using var context = ApiEndpointContext.Create<EchoFormEndpoint>();

        // Act
        var result = await context.WithFormField("name", "Alice").InvokeAsync();

        // Assert
        var value = Assert.IsType<IValueHttpResult>(result, exactMatch: false);
        Assert.Equal("Alice", value.Value);
    }

    [Fact]
    public async Task WithFormFields_WhenSetMultiple_ShouldApplyAllToFormCollection()
    {
        // Arrange
        await using var context = ApiEndpointContext.Create<EchoFormEndpoint>();

        // Act
        context.WithFormFields(new Dictionary<string, string> { ["name"] = "Alice", ["email"] = "alice@example.com" });

        // Assert
        Assert.Equal("Alice", context.HttpContext.Request.Form["name"]);
        Assert.Equal("alice@example.com", context.HttpContext.Request.Form["email"]);
    }

    [Fact]
    public async Task WithFormFile_WhenSet_ShouldBindToHandler()
    {
        // Arrange
        await using var context = ApiEndpointContext.Create<EchoFormFileEndpoint>();

        // Act
        var result = await context.WithFormFile("file", [1, 2, 3], "test.pdf", "application/pdf").InvokeAsync();

        // Assert
        var value = Assert.IsType<IValueHttpResult>(result, exactMatch: false);
        Assert.Equal("test.pdf", value.Value);
    }

    [Fact]
    public async Task WithCancellationToken_WhenCancelled_ShouldPropagateToHandler()
    {
        // Arrange
        await using var context = ApiEndpointContext.Create<CancellableEndpoint>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await context.WithCancellationToken(cts.Token).InvokeAsync();

        // Assert
        var value = Assert.IsType<IValueHttpResult>(result, exactMatch: false);
        Assert.Equal(true, value.Value);
    }

    [Fact]
    public async Task With_WhenChained_ShouldApplyAll()
    {
        // Arrange
        await using var context = ApiEndpointContext.Create<EchoQueryEndpoint>();

        // Act
        var result = await context
            .WithQueryParam("name", "Alice")
            .WithHeader("X-Correlation-Id", "abc-123")
            .InvokeAsync();

        // Assert
        var value = Assert.IsType<IValueHttpResult>(result, exactMatch: false);
        Assert.Equal("Alice", value.Value);
        Assert.Equal("abc-123", context.HttpContext.Request.Headers["X-Correlation-Id"]);
    }

    [Fact]
    public async Task ReadJsonBodyAsync_WhenCalled_ShouldReturnResponseBodyAsRawJsonString()
    {
        // Arrange
        await using var context = ApiEndpointContext.Create<TestEndpoint>();
        await context.InvokeAsync();

        // Act
        var body = await context.ReadJsonBodyAsync();

        // Assert
        Assert.Equal("\"hi\"", body);
    }

    [Fact]
    public async Task ReadJsonBodyAsync_WhenCalledWithType_ShouldDeserializeResponseBody()
    {
        // Arrange
        await using var context = ApiEndpointContext.Create<TestEndpoint>();
        await context.InvokeAsync();

        // Act
        var body = await context.ReadJsonBodyAsync<string>();

        // Assert
        Assert.Equal("hi", body);
    }

    [ApiVersionGroup("versioned-endpoint")]
    [ApiVersion(2, Deprecated = true)]
    private sealed class VersionedEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
            => app.MapGet("/versioned", () => Results.Ok());
    }

    private sealed class TaggedEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
            => app.MapGet("/tagged", () => Results.Ok())
                .WithTags("Products");
    }

    private sealed class TestEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
            => app.MapGet("/test", () => Results.Ok("hi"));
    }

    private sealed class PostEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
            => app.MapPost("/create", () => Results.Ok());
    }

    private sealed class ProtectedEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
            => app.MapGet("/protected", () => Results.Ok())
                .RequireAuthorization();
    }

    private sealed class AnonymousEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
            => app.MapGet("/anonymous", () => Results.Ok())
                .RequireAuthorization()
                .AllowAnonymous();
    }

    private sealed class ServiceEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
            => app.MapGet("/greet", (IGreeter greeter) => Results.Ok(greeter.Greet()));
    }

    private sealed class PlainValueEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
            => app.MapGet("/value", () => "hello");
    }

    private sealed class VoidEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
            => app.MapGet("/nothing", () => { });
    }

    private interface IGreeter
    {
        string Greet();
    }

    private sealed class HelloGreeter : IGreeter
    {
        public int CallCount { get; set; }

        public string Greet()
        {
            CallCount++;
            return "hello";
        }
    }

    private sealed class DisposableService : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    private sealed class EchoRouteEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
            => app.MapGet("/echo-route/{id}", (string id)
                => Results.Ok(id));
    }

    private sealed class EchoQueryEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
            => app.MapGet("/echo-query", ([FromQuery] string name)
                => Results.Ok(name));
    }

    private sealed class EchoHeaderEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
            => app.MapGet("/echo-header", ([FromHeader(Name = "X-Name")] string name)
                => Results.Ok(name));
    }

    private sealed class EchoJsonBodyEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
            => app.MapPost("/echo-json", ([FromBody] Payload payload)
                => Results.Ok(payload.Name));
    }

    private sealed class EchoXmlBodyEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
            => app.MapPost("/echo-xml", async (HttpRequest request) =>
            {
                var body = await new StreamReader(request.Body).ReadToEndAsync();
                return Results.Ok(body);
            });
    }

    private sealed class EchoFormEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
            => app.MapPost("/echo-form", ([FromForm] string name)
                => Results.Ok(name));
    }

    private sealed class EchoFormFileEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
            => app.MapPost("/echo-file", (IFormFile file) => Results.Ok(file.FileName));
    }

    private sealed class CancellableEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
            => app.MapGet("/cancellable", (CancellationToken ct)
                => Results.Ok(ct.IsCancellationRequested));
    }

    private record Payload(string Name);
}
