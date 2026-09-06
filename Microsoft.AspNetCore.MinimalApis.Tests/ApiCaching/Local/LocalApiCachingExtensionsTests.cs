using Microsoft.AspNetCore.MinimalApis.ApiCaching.Local;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Options;
using System.Net;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiCaching.Local;

public sealed class LocalApiCachingExtensionsTests
{
    [Fact]
    public void AddLocalApiCaching_WhenServicesIsNull_ShouldThrow()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        void Act()
            => services.AddLocalApiCaching();

        // Assert
        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void AddLocalApiCaching_WhenCalled_ShouldRegisterOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLocalApiCaching();

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<LocalApiCachingOptions>>();
        Assert.NotNull(options.Value);
    }

    [Fact]
    public void AddLocalApiCaching_WhenConfigured_ShouldApplyOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var expected = TimeSpan.FromMinutes(10);

        // Act
        services.AddLocalApiCaching(options => options.CacheDuration = expected);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<LocalApiCachingOptions>>();
        Assert.Equal(expected, options.Value.CacheDuration);
    }

    [Fact]
    public void WithLocalApiCaching_WhenBuilderIsNull_ShouldThrow()
    {
        // Arrange
        RouteHandlerBuilder builder = null!;

        // Act
        void Act()
            => builder.WithLocalApiCaching();

        // Assert
        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void WithLocalApiCaching_WhenCalled_ShouldReturnBuilder()
    {
        // Arrange
        var app = WebApplication.CreateBuilder().Build();
        var routeBuilder = app.MapGet("/test", () => "result");

        // Act
        var result = routeBuilder.WithLocalApiCaching();

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void WithLocalApiCaching_WithConfigure_WhenBuilderIsNull_ShouldThrow()
    {
        // Arrange
        RouteHandlerBuilder builder = null!;

        // Act
        void Act()
            => builder.WithLocalApiCaching(options => { });

        // Assert
        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void WithLocalApiCaching_WithConfigure_WhenConfigureIsNull_ShouldThrow()
    {
        // Arrange
        var app = WebApplication.CreateBuilder().Build();
        var routeBuilder = app.MapGet("/test", () => "result");

        // Act
        void Act()
            => routeBuilder.WithLocalApiCaching(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void WithLocalApiCaching_WithConfigure_WhenCalled_ShouldReturnBuilder()
    {
        // Arrange
        var app = WebApplication.CreateBuilder().Build();
        var routeBuilder = app.MapGet("/test", () => "result");

        // Act
        var result = routeBuilder.WithLocalApiCaching(options
            => options.CacheDuration = TimeSpan.FromMinutes(99)
        );

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task WithLocalApiCaching_WhenRequestMadeToEndpoint_ShouldApplyCachingFilter()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLocalApiCaching();
        var app = builder.Build();
        app.MapGet("/lcache", () => Results.Ok()).WithLocalApiCaching();
        await app.StartAsync();

        // Act
        var response = await app.GetTestClient().GetAsync("/lcache");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.StopAsync();
    }

    [Fact]
    public async Task WithLocalApiCaching_WithConfigure_WhenRequestMadeToEndpoint_ShouldApplyCachingFilter()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLocalApiCaching();
        var app = builder.Build();
        app.MapGet("/lcache", () => Results.Ok())
            .WithLocalApiCaching(options => options.CacheDuration = TimeSpan.FromMinutes(1));
        await app.StartAsync();

        // Act
        var response = await app.GetTestClient().GetAsync("/lcache");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.StopAsync();
    }
}
