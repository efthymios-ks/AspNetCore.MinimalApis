using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;
using Microsoft.AspNetCore.TestHost;
using System.Net;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiEndpoints;

public sealed class NonResultReturnEndpointTests
{
    [Fact]
    public async Task Endpoint_WhenHandlerReturnsString_ShouldRespondWithPlainText()
    {
        // Arrange
        await using var app = await CreateAppAsync(typeof(StringReturnEndpoint));
        var client = app.GetTestClient();

        // Act
        var response = await client.GetAsync("/return-string");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("hello", body);
    }

    [Fact]
    public async Task Endpoint_WhenHandlerReturnsInt_ShouldRespondWithJsonNumber()
    {
        // Arrange
        await using var app = await CreateAppAsync(typeof(IntReturnEndpoint));
        var client = app.GetTestClient();

        // Act
        var response = await client.GetAsync("/return-int");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("42", body);
    }

    [Fact]
    public async Task Endpoint_WhenHandlerReturnsDto_ShouldRespondWithJsonObject()
    {
        // Arrange
        await using var app = await CreateAppAsync(typeof(DtoReturnEndpoint));
        var client = app.GetTestClient();

        // Act
        var response = await client.GetAsync("/return-dto");
        var widget = await response.Content.ReadFromJsonAsync<Widget>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new Widget(1, "gadget"), widget);
    }

    private static async Task<WebApplication> CreateAppAsync(params Type[] endpointTypes)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        foreach (var endpointType in endpointTypes)
        {
            builder.Services.AddTransient(typeof(ApiEndpoint), endpointType);
        }

        builder.Services.AddEndpointsApiExplorer();

        var app = builder.Build();
        await app.UseApiEndpointsAsync();
        await app.StartAsync();
        return app;
    }

    public sealed record Widget(int Id, string Name);

    public sealed class StringReturnEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
            => endpointRouteBuilder.MapGet("/return-string", () => "hello");
    }

    public sealed class IntReturnEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
            => endpointRouteBuilder.MapGet("/return-int", () => 42);
    }

    public sealed class DtoReturnEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
            => endpointRouteBuilder.MapGet("/return-dto", () => new Widget(1, "gadget"));
    }
}
