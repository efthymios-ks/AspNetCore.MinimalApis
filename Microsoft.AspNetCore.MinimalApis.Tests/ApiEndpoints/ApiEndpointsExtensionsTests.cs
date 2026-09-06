using Asp.Versioning;
using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;
using Microsoft.AspNetCore.MinimalApis.ApiVersions;
using Microsoft.AspNetCore.TestHost;
using System.Net;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiEndpoints;

public sealed class ApiEndpointsExtensionsTests
{
    [Fact]
    public void AddApiEndpoints_WhenServicesIsNull_ShouldThrow()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        void Act()
            => services.AddApiEndpoints(typeof(SimpleEndpoint).Assembly);

        // Assert
        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void AddApiEndpoints_WithAssemblies_WhenEndpointsExist_ShouldRegister()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApiEndpoints(typeof(ApiEndpointsExtensionsTests).Assembly);

        // Assert
        var provider = services.BuildServiceProvider();
        var endpoints = provider
            .GetServices<ApiEndpoint>()
            .ToArray();
        Assert.Contains(endpoints, endpoint => endpoint is SimpleEndpoint);
    }

    [Fact]
    public async Task UseApiEndpointsAsync_WhenAppIsNull_ShouldThrow()
    {
        // Arrange
        WebApplication app = null!;

        // Act
        Task Act()
            => app.UseApiEndpointsAsync();

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(Act);
    }

    [Fact]
    public async Task UseApiEndpointsAsync_WhenNonVersionedEndpoints_ShouldMapThem()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        RegisterEndpoints(builder.Services, typeof(SimpleEndpoint));
        var app = builder.Build();
        await app.UseApiEndpointsAsync();
        await app.StartAsync();

        // Act
        var response = await app.GetTestClient().GetAsync("/simple");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.StopAsync();
    }

    [Fact]
    public async Task UseApiEndpointsAsync_WhenVersionedEndpoints_ShouldMapWithVersionPrefix()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        RegisterEndpoints(builder.Services, typeof(VersionedEndpoint));
        RegisterVersioning(builder.Services);
        var app = builder.Build();
        await app.UseApiEndpointsAsync();
        await app.StartAsync();

        // Act
        var response = await app.GetTestClient().GetAsync("/v1/versioned");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.StopAsync();
    }

    [Fact]
    public async Task UseApiEndpointsAsync_WhenVersionedEndpoint_ShouldNotBeReachableWithoutVersionPrefix()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        RegisterEndpoints(builder.Services, typeof(VersionedEndpoint));
        RegisterVersioning(builder.Services);
        var app = builder.Build();
        await app.UseApiEndpointsAsync();
        await app.StartAsync();

        // Act
        var response = await app.GetTestClient().GetAsync("/versioned");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await app.StopAsync();
    }

    [Fact]
    public async Task UseApiEndpointsAsync_WhenRoutePrefixAndVersionedEndpoint_ShouldMapWithPrefixAndVersion()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        RegisterEndpoints(builder.Services, typeof(VersionedEndpoint));
        RegisterVersioning(builder.Services);
        var app = builder.Build();
        await app.UseApiEndpointsAsync(options => options.RoutePrefix = "/api");
        await app.StartAsync();

        // Act
        var response = await app.GetTestClient().GetAsync("/api/v1/versioned");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.StopAsync();
    }

    [Fact]
    public async Task UseApiEndpointsAsync_WhenActiveVersion_ShouldReportBothSupportedAndDeprecatedHeaders()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        RegisterEndpoints(builder.Services, typeof(ActiveVersionEndpoint), typeof(DeprecatedVersionEndpoint));
        RegisterVersioning(builder.Services);
        var app = builder.Build();
        await app.UseApiEndpointsAsync();
        await app.StartAsync();

        // Act
        var response = await app.GetTestClient().GetAsync("/v2/multi-version");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains(ApiVersionHeaders.Supported));
        Assert.Contains("2", response.Headers.GetValues(ApiVersionHeaders.Supported));
        Assert.True(response.Headers.Contains(ApiVersionHeaders.Deprecated));
        Assert.Contains("1", response.Headers.GetValues(ApiVersionHeaders.Deprecated));
        Assert.DoesNotContain("2", response.Headers.GetValues(ApiVersionHeaders.Deprecated));
        await app.StopAsync();
    }

    [Fact]
    public async Task UseApiEndpointsAsync_WhenDeprecatedVersion_ShouldReportBothSupportedAndDeprecatedHeaders()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        RegisterEndpoints(builder.Services, typeof(ActiveVersionEndpoint), typeof(DeprecatedVersionEndpoint));
        RegisterVersioning(builder.Services);
        var app = builder.Build();
        await app.UseApiEndpointsAsync();
        await app.StartAsync();

        // Act
        var response = await app.GetTestClient().GetAsync("/v1/multi-version");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains(ApiVersionHeaders.Supported));
        Assert.Contains("2", response.Headers.GetValues(ApiVersionHeaders.Supported));
        Assert.True(response.Headers.Contains(ApiVersionHeaders.Deprecated));
        Assert.Contains("1", response.Headers.GetValues(ApiVersionHeaders.Deprecated));
        Assert.DoesNotContain("1", response.Headers.GetValues(ApiVersionHeaders.Supported));
        await app.StopAsync();
    }

    [Fact]
    public async Task UseApiEndpointsAsync_WhenNonVersionedEndpoint_ShouldNotEmitVersionHeaders()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        RegisterEndpoints(builder.Services, typeof(SimpleEndpoint));
        var app = builder.Build();
        await app.UseApiEndpointsAsync();
        await app.StartAsync();

        // Act
        var response = await app.GetTestClient().GetAsync("/simple");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains(ApiVersionHeaders.Supported));
        Assert.False(response.Headers.Contains(ApiVersionHeaders.Deprecated));
        await app.StopAsync();
    }

    [Fact]
    public async Task UseApiEndpointsAsync_WhenEndpointHasVersionGroupButNoVersion_ShouldThrow()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        RegisterEndpoints(builder.Services, typeof(MissingVersionAttributeEndpoint));
        RegisterVersioning(builder.Services);
        var app = builder.Build();

        // Act
        Task Act()
            => app.UseApiEndpointsAsync();

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(Act);
    }

    [Fact]
    public async Task UseApiEndpointsAsync_WhenMinorVersionSpecified_ShouldThrow()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        RegisterEndpoints(builder.Services, typeof(MinorVersionEndpoint));
        RegisterVersioning(builder.Services);
        var app = builder.Build();

        // Act
        Task Act()
            => app.UseApiEndpointsAsync();

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(Act);
    }

    [Fact]
    public async Task UseApiEndpointsAsync_WhenDuplicateVersionInSameGroup_ShouldThrow()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        RegisterEndpoints(builder.Services, typeof(DuplicateVersionEndpointA), typeof(DuplicateVersionEndpointB));
        RegisterVersioning(builder.Services);
        var app = builder.Build();

        // Act
        Task Act()
            => app.UseApiEndpointsAsync();

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(Act);
    }

    [Fact]
    public async Task UseApiEndpointsAsync_WhenRoutePrefixSet_ShouldPrefixRoutes()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        RegisterEndpoints(builder.Services, typeof(SimpleEndpoint));
        var app = builder.Build();
        await app.UseApiEndpointsAsync(options => options.RoutePrefix = "/api");
        await app.StartAsync();

        // Act
        var response = await app.GetTestClient().GetAsync("/api/simple");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.StopAsync();
    }

    [Fact]
    public async Task UseApiEndpointsAsync_WhenGlobalFilterTypeRegistered_ShouldApplyFilter()
    {
        // Arrange
        TrackingFilter.Reset();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        RegisterEndpoints(builder.Services, typeof(SimpleEndpoint));
        var app = builder.Build();
        await app.UseApiEndpointsAsync(options => options.AddGlobalEndpointFilter<TrackingFilter>());
        await app.StartAsync();

        // Act
        await app.GetTestClient().GetAsync("/simple");

        // Assert
        Assert.True(TrackingFilter.WasInvoked);
        await app.StopAsync();
    }

    [Fact]
    public async Task UseApiEndpointsAsync_WhenGlobalFilterFactoryRegistered_ShouldApplyFilterFactory()
    {
        // Arrange
        TrackingFilterFactory.Reset();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        RegisterEndpoints(builder.Services, typeof(SimpleEndpoint));
        var app = builder.Build();
        await app.UseApiEndpointsAsync(options
            => options.AddGlobalEndpointFilter(TrackingFilterFactory.Factory));
        await app.StartAsync();

        // Act
        await app.GetTestClient().GetAsync("/simple");

        // Assert
        Assert.True(TrackingFilterFactory.WasInvoked);
        await app.StopAsync();
    }

    private static void RegisterEndpoints(IServiceCollection services, params Type[] endpointTypes)
    {
        foreach (var type in endpointTypes)
        {
            services.AddTransient(typeof(ApiEndpoint), type);
        }

        services.AddEndpointsApiExplorer();
    }

    private static void RegisterVersioning(IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new(1);
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlPrefixApiVersionReader();
        });
    }

    public sealed class SimpleEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
            => endpointRouteBuilder.MapGet("/simple", () => Results.Ok("simple"));
    }

    [ApiVersionGroup("test-group")]
    [ApiVersion("1.0")]
    public sealed class VersionedEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
            => endpointRouteBuilder.MapGet("/versioned", () => Results.Ok("versioned"));
    }

    [ApiVersionGroup("multi-version-group")]
    [ApiVersion("2.0")]
    public sealed class ActiveVersionEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
            => endpointRouteBuilder.MapGet("/multi-version", () => Results.Ok("v2"));
    }

    [ApiVersionGroup("multi-version-group")]
    [ApiVersion("1.0", Deprecated = true)]
    public sealed class DeprecatedVersionEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
            => endpointRouteBuilder.MapGet("/multi-version", () => Results.Ok("v1"));
    }

    [ApiVersionGroup("test-group")]
    public sealed class MissingVersionAttributeEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
            => endpointRouteBuilder.MapGet("/missing", () => Results.Ok());
    }

    [ApiVersionGroup("test-group")]
    [ApiVersion("1.1")]
    public sealed class MinorVersionEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
            => endpointRouteBuilder.MapGet("/minor", () => Results.Ok());
    }

    [ApiVersionGroup("dup-group")]
    [ApiVersion("1.0")]
    public sealed class DuplicateVersionEndpointA : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
            => endpointRouteBuilder.MapGet("/dup-a", () => Results.Ok());
    }

    [ApiVersionGroup("dup-group")]
    [ApiVersion("1.0")]
    public sealed class DuplicateVersionEndpointB : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
            => endpointRouteBuilder.MapGet("/dup-b", () => Results.Ok());
    }

    public sealed class TrackingFilter : IEndpointFilter
    {
        public static bool WasInvoked { get; private set; }

        public static void Reset()
            => WasInvoked = false;

        public async ValueTask<object?> InvokeAsync(
            EndpointFilterInvocationContext context,
            EndpointFilterDelegate next
        )
        {
            WasInvoked = true;
            return await next(context);
        }
    }

    public sealed class TrackingFilterFactory
    {
        public static bool WasInvoked { get; private set; }

        public static void Reset()
            => WasInvoked = false;

        public static EndpointFilterDelegate Factory(EndpointFilterFactoryContext _, EndpointFilterDelegate next)
            => async invocationContext =>
            {
                WasInvoked = true;
                return await next(invocationContext);
            };
    }
}
