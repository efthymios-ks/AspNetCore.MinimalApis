using Microsoft.AspNetCore.MinimalApis.ApiIdempotency;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Options;
using System.Net;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiIdempotency;

public sealed class ApiIdempotencyExtensionsTests
{
    [Fact]
    public void AddApiIdempotency_WhenServicesIsNull_ShouldThrow()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        void Act()
            => services.AddApiIdempotency<DistributedCacheApiIdempotencyStore>();

        // Assert
        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void AddApiIdempotency_WhenCalled_ShouldRegisterStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();

        // Act
        services.AddApiIdempotency();

        // Assert
        var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<IApiIdempotencyStore>();
        Assert.IsType<DistributedCacheApiIdempotencyStore>(store);
    }

    [Fact]
    public void AddApiIdempotency_WhenConfigured_ShouldApplyOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var expected = TimeSpan.FromMinutes(20);

        // Act
        services.AddApiIdempotency(options => options.CacheDuration = expected);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ApiIdempotencyOptions>>();
        Assert.Equal(expected, options.Value.CacheDuration);
    }

    [Fact]
    public void WithApiIdempotency_WhenBuilderIsNull_ShouldThrow()
    {
        // Arrange
        RouteHandlerBuilder builder = null!;

        // Act
        void Act()
            => builder.WithApiIdempotency();

        // Assert
        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void WithApiIdempotency_WhenCalled_ShouldReturnBuilder()
    {
        // Arrange
        var app = WebApplication.CreateBuilder().Build();
        var routeBuilder = app.MapPost("/test", () => "result");

        // Act
        var result = routeBuilder.WithApiIdempotency();

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void WithApiIdempotency_WithConfigure_WhenBuilderIsNull_ShouldThrow()
    {
        // Arrange
        RouteHandlerBuilder builder = null!;

        // Act
        void Act()
            => builder.WithApiIdempotency(_ => { });

        // Assert
        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void WithApiIdempotency_WithConfigure_WhenConfigureIsNull_ShouldThrow()
    {
        // Arrange
        var app = WebApplication.CreateBuilder().Build();
        var routeBuilder = app.MapPost("/test", () => "result");

        // Act
        void Act()
            => routeBuilder.WithApiIdempotency(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void WithApiIdempotency_WithConfigure_WhenCalled_ShouldReturnBuilder()
    {
        // Arrange
        var app = WebApplication.CreateBuilder().Build();
        var routeBuilder = app.MapPost("/test", () => "result");

        // Act
        var result = routeBuilder.WithApiIdempotency(options
            => options.CacheDuration = TimeSpan.FromMinutes(5)
        );

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task WithApiIdempotency_WhenRequestMadeToEndpoint_ShouldApplyFilter()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddApiIdempotency(
            options => options.KeySuffixFactory = _ => "key");
        var app = builder.Build();
        app.MapPost("/idem", () => Results.Ok()).WithApiIdempotency();
        await app.StartAsync();

        // Act
        var response = await app.GetTestClient().PostAsync("/idem", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.StopAsync();
    }

    [Fact]
    public async Task WithApiIdempotency_WhenNoDistributedCacheRegistered_ShouldThrowInsightfulError()
    {
        // Arrange — idempotency enabled but no IDistributedCache registered.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Production"
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddApiIdempotency(options => options.KeySuffixFactory = _ => "key");
        var app = builder.Build();
        app.MapPost("/idem", () => Results.Ok()).WithApiIdempotency();
        await app.StartAsync();

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => app.GetTestClient().PostAsync("/idem", null)
        );

        // Assert
        Assert.Contains("IDistributedCache", exception.Message);
        await app.StopAsync();
    }

    [Fact]
    public async Task WithApiIdempotency_WithConfigure_WhenRequestMadeToEndpoint_ShouldApplyFilter()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddApiIdempotency();
        var app = builder.Build();
        app.MapPost("/idem", () => Results.Ok())
            .WithApiIdempotency(options =>
            {
                options.CacheDuration = TimeSpan.FromMinutes(1);
                options.KeySuffixFactory = _ => "key";
            });
        await app.StartAsync();

        // Act
        var response = await app.GetTestClient().PostAsync("/idem", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.StopAsync();
    }
}
