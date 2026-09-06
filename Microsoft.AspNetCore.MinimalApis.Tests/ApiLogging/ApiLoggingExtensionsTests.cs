using Microsoft.AspNetCore.MinimalApis.ApiLogging;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Options;
using System.Collections.Immutable;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiLogging;

public sealed class ApiLoggingExtensionsTests
{
    [Fact]
    public void AddApiRequestLog_WhenServicesIsNull_ShouldThrow()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddApiRequestLog());
    }

    [Fact]
    public void AddApiRequestLog_WhenCalled_ShouldRegisterOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApiRequestLog();

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<GlobalLogScopeOptions>>();
        Assert.NotNull(options.Value);
    }

    [Fact]
    public void AddApiRequestLog_WhenConfigured_ShouldApplyOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<HttpContext, IReadOnlyDictionary<string, object?>> selector = httpContext
            => new Dictionary<string, object?> { ["key"] = "value" };

        // Act
        services.AddApiRequestLog(o => o.PropertiesSelector = selector);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<GlobalLogScopeOptions>>();
        Assert.Same(selector, options.Value.PropertiesSelector);
    }

    [Fact]
    public void UseApiRequestLog_WhenAppIsNull_ShouldThrow()
    {
        // Arrange
        IApplicationBuilder app = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(app.UseApiRequestLog);
    }

    [Fact]
    public void UseApiRequestLog_WhenCalled_ShouldReturnApp()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddApiRequestLog();
        builder.Services.AddLogging();
        var app = builder.Build();

        // Act
        var result = app.UseApiRequestLog();

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void WithAdditionalLogProperties_WhenBuilderIsNull_ShouldThrow()
    {
        // Arrange
        RouteHandlerBuilder builder = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithAdditionalLogProperties(httpContext => { }));
    }

    [Fact]
    public void WithAdditionalLogProperties_WhenConfigureIsNull_ShouldThrow()
    {
        // Arrange
        var app = WebApplication.CreateBuilder().Build();
        var routeBuilder = app.MapGet("/test", () => "result");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => routeBuilder.WithAdditionalLogProperties(null!));
    }

    [Fact]
    public void WithAdditionalLogProperties_WhenCalled_ShouldReturnBuilder()
    {
        // Arrange
        var app = WebApplication.CreateBuilder().Build();
        var routeBuilder = app.MapGet("/test", () => "result");

        // Act
        var result = routeBuilder.WithAdditionalLogProperties(httpContext => { });

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task WithAdditionalLogProperties_WhenPropertiesProvided_ShouldSetInContextItems()
    {
        // Arrange
        Dictionary<string, object?>? capturedItems = null;
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        var routeBuilder = app.MapGet("/test", (HttpContext context) =>
        {
            capturedItems = context.Items[EndpointLogScopeOptions.Key] as Dictionary<string, object?>;
            return Results.Ok();
        });
        routeBuilder.WithAdditionalLogProperties(options =>
            options.PropertiesSelector = _ => new Dictionary<string, object?>
            {
                ["prop"] = "val"
            });
        await app.StartAsync();

        // Act
        await app.GetTestClient().GetAsync("/test");

        // Assert
        Assert.NotNull(capturedItems);
        Assert.True(capturedItems.ContainsKey("prop"));
        await app.StopAsync();
    }

    [Fact]
    public async Task WithAdditionalLogProperties_WhenPropertiesReturnNull_ShouldNotSetInContextItems()
    {
        // Arrange
        var contextItemsContainKey = false;
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        var routeBuilder = app.MapGet("/test", (HttpContext context) =>
        {
            contextItemsContainKey = context.Items.ContainsKey(EndpointLogScopeOptions.Key);
            return Results.Ok();
        });
        routeBuilder.WithAdditionalLogProperties(options =>
            options.PropertiesSelector = _ => null!);
        await app.StartAsync();

        // Act
        await app.GetTestClient().GetAsync("/test");

        // Assert
        Assert.False(contextItemsContainKey);
        await app.StopAsync();
    }

    [Fact]
    public async Task WithAdditionalLogProperties_WhenPropertiesReturnEmpty_ShouldNotSetInContextItems()
    {
        // Arrange
        var contextItemsContainKey = false;
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        var routeBuilder = app.MapGet("/test", (HttpContext context) =>
        {
            contextItemsContainKey = context.Items.ContainsKey(EndpointLogScopeOptions.Key);
            return Results.Ok();
        });
        routeBuilder.WithAdditionalLogProperties(options =>
            options.PropertiesSelector = _ => ImmutableDictionary<string, object?>.Empty);
        await app.StartAsync();

        // Act
        await app.GetTestClient().GetAsync("/test");

        // Assert
        Assert.False(contextItemsContainKey);
        await app.StopAsync();
    }
}
