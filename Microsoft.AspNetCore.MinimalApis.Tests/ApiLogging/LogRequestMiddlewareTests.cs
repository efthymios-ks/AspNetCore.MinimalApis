using Microsoft.AspNetCore.MinimalApis.ApiLogging;
using Microsoft.Extensions.Options;
using System.Net;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiLogging;

public sealed class LogRequestMiddlewareTests
{
    private readonly TestLogger _logger = new();

    private LogRequestMiddleware BuildMiddleware(Action<GlobalLogScopeOptions>? configure = null)
    {
        var options = new GlobalLogScopeOptions();
        configure?.Invoke(options);
        return new(
            context => Task.CompletedTask,
            _logger,
            Options.Create(options)
        );
    }

    [Fact]
    public async Task InvokeAsync_WhenStatusCode200_ShouldLogInformation()
    {
        // Arrange
        var middleware = BuildMiddleware();
        var context = BuildContext(StatusCodes.Status200OK);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Contains(_logger.Entries, entry => entry.Level is LogLevel.Information);
    }

    [Fact]
    public async Task InvokeAsync_WhenStatusCode400_ShouldLogWarning()
    {
        // Arrange
        var middleware = new LogRequestMiddleware(
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Task.CompletedTask;
            },
            _logger,
            Options.Create(new GlobalLogScopeOptions())
        );
        var context = BuildContext(StatusCodes.Status200OK);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Contains(_logger.Entries, entry => entry.Level is LogLevel.Warning);
    }

    [Fact]
    public async Task InvokeAsync_WhenStatusCode500_ShouldLogError()
    {
        // Arrange
        var middleware = new LogRequestMiddleware(
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return Task.CompletedTask;
            },
            _logger,
            Options.Create(new GlobalLogScopeOptions())
        );
        var context = BuildContext(StatusCodes.Status200OK);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Contains(_logger.Entries, entry => entry.Level is LogLevel.Error);
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionThrown_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var middleware = new LogRequestMiddleware(
            context => throw new InvalidOperationException("boom"),
            _logger,
            Options.Create(new GlobalLogScopeOptions())
        );

        var context = BuildContext(StatusCodes.Status200OK);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
        Assert.Contains(_logger.Entries, entry => entry.Level is LogLevel.Error);
    }

    [Fact]
    public async Task InvokeAsync_WhenGlobalPropertiesConfigured_ShouldIncludeInScope()
    {
        // Arrange
        var middleware = BuildMiddleware(
            options => options.PropertiesSelector = httpContext
                => new Dictionary<string, object?>
                {
                    ["tenant"] = "abc"
                }
        );

        var context = BuildContext(StatusCodes.Status200OK);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Contains(_logger.Entries, entry => entry.Level is LogLevel.Information);
    }

    [Fact]
    public async Task InvokeAsync_WhenEndpointPropertiesInHttpContextItems_ShouldIncludeInScope()
    {
        // Arrange
        var middleware = BuildMiddleware();
        var context = BuildContext(StatusCodes.Status200OK);
        context.Items[EndpointLogScopeOptions.Key] = new Dictionary<string, object?>
        {
            ["requestId"] = "req-1"
        };

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Contains(_logger.Entries, entry => entry.Level is LogLevel.Information);
    }

    [Fact]
    public async Task InvokeAsync_WhenConnectionHasNoIpAddresses_ShouldNotAddIpProps()
    {
        // Arrange
        var middleware = BuildMiddleware();
        var context = BuildContext(StatusCodes.Status200OK);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.DoesNotContain(_logger.Entries, entry => entry.Message?.Contains("IpAddress") is true);
    }

    [Fact]
    public async Task InvokeAsync_WhenStatusCode201_ShouldLogInformation()
    {
        // Arrange
        var middleware = new LogRequestMiddleware(
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status201Created;
                return Task.CompletedTask;
            },
            _logger,
            Options.Create(new GlobalLogScopeOptions())
        );
        var context = BuildContext(StatusCodes.Status200OK);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Contains(_logger.Entries, entry => entry.Level is LogLevel.Information);
    }

    [Fact]
    public async Task InvokeAsync_WhenStatusCode399_ShouldLogInformation()
    {
        // Arrange
        var middleware = new LogRequestMiddleware(
            context =>
            {
                context.Response.StatusCode = 399;
                return Task.CompletedTask;
            },
            _logger,
            Options.Create(new GlobalLogScopeOptions())
        );
        var context = BuildContext(StatusCodes.Status200OK);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Contains(_logger.Entries, entry => entry.Level is LogLevel.Information);
    }

    [Fact]
    public async Task InvokeAsync_WhenStatusCode499_ShouldLogWarning()
    {
        // Arrange
        var middleware = new LogRequestMiddleware(
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
                return Task.CompletedTask;
            },
            _logger,
            Options.Create(new GlobalLogScopeOptions())
        );
        var context = BuildContext(StatusCodes.Status200OK);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Contains(_logger.Entries, entry => entry.Level is LogLevel.Warning);
    }

    [Fact]
    public async Task InvokeAsync_WhenConnectionHasLocalIpAddress_ShouldLogConnectionProperties()
    {
        // Arrange
        var middleware = BuildMiddleware();
        var context = BuildContext(StatusCodes.Status200OK);
        context.Connection.LocalIpAddress = IPAddress.Loopback;
        context.Connection.LocalPort = 5000;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Contains(_logger.Entries, entry => entry.Level is LogLevel.Information);
    }

    [Fact]
    public async Task InvokeAsync_WhenConnectionHasRemoteIpAddress_ShouldLogConnectionProperties()
    {
        // Arrange
        var middleware = BuildMiddleware();
        var context = BuildContext(StatusCodes.Status200OK);
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        context.Connection.RemotePort = 6000;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Contains(_logger.Entries, entry => entry.Level is LogLevel.Information);
    }

    private static DefaultHttpContext BuildContext(int statusCode)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Body = new MemoryStream();
        context.Response.StatusCode = statusCode;
        return context;
    }

    private sealed class TestLogger : ILogger<LogRequestMiddleware>
    {
        public List<(LogLevel Level, string? Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
