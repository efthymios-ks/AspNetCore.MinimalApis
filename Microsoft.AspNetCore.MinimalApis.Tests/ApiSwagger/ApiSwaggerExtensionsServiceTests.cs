using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;
using Microsoft.AspNetCore.MinimalApis.ApiSwagger;
using Microsoft.OpenApi;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiSwagger;

public sealed class ApiSwaggerExtensionsServiceTests
{
    [Fact]
    public async Task ConfigureApiSwaggerAsync_WhenAppIsNull_ShouldThrow()
    {
        // Arrange
        WebApplication app = null!;

        // Act
        Task Act()
            => app.ConfigureApiSwaggerAsync(typeof(ApiSwaggerExtensionsServiceTests).Assembly);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(Act);
    }

    [Fact]
    public async Task ConfigureApiSwaggerAsync_WhenNoEndpoints_ShouldSucceed()
    {
        // Arrange
        var app = WebApplication.CreateBuilder().Build();

        // Act
        var exception = await Record.ExceptionAsync(() => app.ConfigureApiSwaggerAsync(typeof(object).Assembly));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task ConfigureApiSwaggerAsync_WhenEndpointHasParameterAttribute_ShouldCallConfigureAsync()
    {
        // Arrange
        ApiParameterBase.ClearConfigurations();
        TrackingApiParamAttribute.Reset();
        var app = WebApplication.CreateBuilder().Build();

        // Act
        await app.ConfigureApiSwaggerAsync(typeof(ApiSwaggerExtensionsServiceTests).Assembly);

        // Assert
        Assert.True(TrackingApiParamAttribute.ConfigureWasCalled);
    }

    [Fact]
    public async Task ConfigureApiSwaggerAsync_WhenAssemblyContainsApiSummary_ShouldCallConfigureAsync()
    {
        // Arrange
        TrackingApiSummary.Reset();
        var app = WebApplication.CreateBuilder().Build();

        // Act
        await app.ConfigureApiSwaggerAsync(typeof(ApiSwaggerExtensionsServiceTests).Assembly);

        // Assert
        Assert.True(TrackingApiSummary.ConfigureWasCalled);
    }

    [TrackingApiParam]
    public sealed class EndpointWithTrackedParam : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
            => endpointRouteBuilder.MapGet("/swagger-param", () => Results.Ok());
    }

    public sealed class EndpointWithTrackedSummary : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
            => endpointRouteBuilder.MapGet("/swagger-summary", () => Results.Ok());
    }

    public sealed class TrackingApiParamAttribute : ApiParameterBase
    {
        public static bool ConfigureWasCalled { get; private set; }

        public static void Reset()
            => ConfigureWasCalled = false;

        public override Microsoft.OpenApi.ParameterLocation Location
            => ParameterLocation.Header;

        public override string Key
            => "X-Tracking";

        public override Task ConfigureAsync(
            IServiceProvider services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            ConfigureWasCalled = true;
            return Task.CompletedTask;
        }
    }

    public sealed class TrackingApiSummary : ApiSummary<EndpointWithTrackedSummary>
    {
        public static bool ConfigureWasCalled { get; private set; }

        public static void Reset()
            => ConfigureWasCalled = false;

        public override Task ConfigureAsync(
            IServiceProvider services,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            ConfigureWasCalled = true;
            return Task.CompletedTask;
        }
    }
}
