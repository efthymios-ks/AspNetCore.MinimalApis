using Microsoft.AspNetCore.MinimalApis.ApiCaching.Distributed;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiCaching.Distributed;

public sealed class DistributedApiCachingExtensionsTests
{
    [Fact]
    public void AddDistributedApiCaching_WhenServicesIsNull_ShouldThrow()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        void Act()
            => services.AddDistributedApiCaching();

        // Assert
        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void AddDistributedApiCaching_WhenCalled_ShouldRegisterOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDistributedApiCaching();

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<DistributedApiCachingOptions>>();
        Assert.NotNull(options.Value);
    }

    [Fact]
    public void AddDistributedApiCaching_WhenConfigured_ShouldApplyOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var expected = TimeSpan.FromMinutes(15);

        // Act
        services.AddDistributedApiCaching(options => options.CacheDuration = expected);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<DistributedApiCachingOptions>>();
        Assert.Equal(expected, options.Value.CacheDuration);
    }

    [Fact]
    public void WithDistributedApiCaching_WhenBuilderIsNull_ShouldThrow()
    {
        // Arrange
        RouteHandlerBuilder builder = null!;

        // Act
        void Act()
            => builder.WithDistributedApiCaching();

        // Assert
        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void WithDistributedApiCaching_WhenCalled_ShouldReturnBuilder()
    {
        // Arrange
        var app = WebApplication.CreateBuilder().Build();
        var routeBuilder = app.MapGet("/test", () => "result");

        // Act
        var result = routeBuilder.WithDistributedApiCaching();

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void WithDistributedApiCaching_WithConfigure_WhenBuilderIsNull_ShouldThrow()
    {
        // Arrange
        RouteHandlerBuilder builder = null!;

        // Act
        void Act()
            => builder.WithDistributedApiCaching(_ => { });

        // Assert
        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void WithDistributedApiCaching_WithConfigure_WhenConfigureIsNull_ShouldThrow()
    {
        // Arrange
        var app = WebApplication.CreateBuilder().Build();
        var routeBuilder = app.MapGet("/test", () => "result");

        // Act
        void Act()
            => routeBuilder.WithDistributedApiCaching(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void WithDistributedApiCaching_WithConfigure_WhenCalled_ShouldReturnBuilder()
    {
        // Arrange
        var app = WebApplication.CreateBuilder().Build();
        var routeBuilder = app.MapGet("/test", () => "result");

        // Act
        var result = routeBuilder.WithDistributedApiCaching(options
            => options.CacheDuration = TimeSpan.FromMinutes(5)
        );

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task WithDistributedApiCaching_WhenRequestMadeToEndpoint_ShouldApplyCachingFilter()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddDistributedApiCaching();
        var app = builder.Build();
        app.MapGet("/dcache", () => Results.Ok()).WithDistributedApiCaching();
        await app.StartAsync();

        // Act
        var response = await app.GetTestClient().GetAsync("/dcache");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.StopAsync();
    }

    [Fact]
    public async Task WithDistributedApiCaching_WithConfigure_WhenRequestMadeToEndpoint_ShouldApplyCachingFilter()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddDistributedApiCaching();
        var app = builder.Build();
        app.MapGet("/dcache", () => Results.Ok())
            .WithDistributedApiCaching(options => options.CacheDuration = TimeSpan.FromMinutes(1));
        await app.StartAsync();

        // Act
        var response = await app.GetTestClient().GetAsync("/dcache");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await app.StopAsync();
    }

    [Fact]
    public async Task WithDistributedApiCaching_WhenReplayingCachedResponse_ShouldSerializeBodyWithHostJsonOptions()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddDistributedApiCaching();
        builder.Services.ConfigureHttpJsonOptions(options
            => options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
        var app = builder.Build();
        app.MapGet("/dcache-casing", () => Results.Ok(new CasingSample(42))).WithDistributedApiCaching();
        await app.StartAsync();
        var client = app.GetTestClient();

        // Act — first call caches (miss), second replays it (hit)
        var live = await client.GetStringAsync("/dcache-casing");
        var replayed = await client.GetStringAsync("/dcache-casing");

        // Assert — the replayed cache-hit body honours the host camelCase options, exactly like the live one
        Assert.Contains("\"sampleValue\":42", live);
        Assert.Contains("\"sampleValue\":42", replayed);
        Assert.DoesNotContain("SampleValue", replayed);

        await app.StopAsync();
    }

    private sealed record CasingSample(int SampleValue);
}
