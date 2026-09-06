using Microsoft.AspNetCore.MinimalApis.ApiIdempotency;
using Microsoft.AspNetCore.MinimalApis.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiIdempotency;

public sealed class ApiIdempotencyFilterTests
{
    private const string KeySuffix = "custom-suffix";
    private readonly IApiIdempotencyStore _store = Substitute.For<IApiIdempotencyStore>();

    private ApiIdempotencyFilter BuildFilter(Action<ApiIdempotencyOptions>? configure = null)
    {
        var options = new ApiIdempotencyOptions
        {
            KeySuffixFactory = _ => KeySuffix
        };
        configure?.Invoke(options);
        return new ApiIdempotencyFilter(
            _store,
            NullLogger<ApiIdempotencyFilter>.Instance,
            Options.Create(options)
        );
    }

    [Fact]
    public async Task InvokeAsync_WhenGetRequest_ShouldReturn405()
    {
        // Arrange
        var filter = BuildFilter();
        var context = BuildContext("GET");

        // Act
        var result = await filter.InvokeAsync(context, Ok);

        // Assert
        var httpResult = (IStatusCodeHttpResult)result!;
        Assert.Equal(StatusCodes.Status405MethodNotAllowed, httpResult.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenIdempotencyKeyMissing_ShouldReturn400()
    {
        // Arrange
        var filter = BuildFilter(options => options.KeySuffixFactory = _ => string.Empty);
        var context = BuildContext("POST");

        // Act
        var result = await filter.InvokeAsync(context, Ok);

        // Assert
        var httpResult = (IStatusCodeHttpResult)result!;
        Assert.Equal(StatusCodes.Status400BadRequest, httpResult.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenReserved_ShouldCallNextAndComplete()
    {
        // Arrange
        _store.TryReserveAsync(default!, default, default)
            .ReturnsForAnyArgs(IdempotencyEntry.Reserved);
        var filter = BuildFilter();
        var context = BuildContext("POST");

        var nextCalled = false;
        ValueTask<object?> Next(EndpointFilterInvocationContext _)
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok("ok"));
        }

        // Act
        await filter.InvokeAsync(context, Next);

        // Assert
        Assert.True(nextCalled);
        await _store.ReceivedWithAnyArgs(1).CompleteAsync(default!, default!, default, default);
        await _store.DidNotReceiveWithAnyArgs().ReleaseAsync(default!, default);
    }

    [Fact]
    public async Task InvokeAsync_WhenCompleted_ShouldReplayStoredResponseWithoutCallingNext()
    {
        // Arrange
        var stored = new CachedResponse(202, "text/plain", "stored"u8.ToArray());
        _store.TryReserveAsync(default!, default, default)
            .ReturnsForAnyArgs(IdempotencyEntry.Completed(stored));
        var filter = BuildFilter();
        var context = BuildContext("POST");

        var nextCalled = false;
        ValueTask<object?> Next(EndpointFilterInvocationContext _)
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok("fresh"));
        }

        // Act
        var result = await filter.InvokeAsync(context, Next);

        // Assert
        Assert.False(nextCalled);
        Assert.NotNull(result);
        Assert.Equal(202, context.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenPending_ShouldReturn409WithoutCallingNext()
    {
        // Arrange
        _store.TryReserveAsync(default!, default, default)
            .ReturnsForAnyArgs(IdempotencyEntry.Pending);
        var filter = BuildFilter();
        var context = BuildContext("POST");

        var nextCalled = false;
        ValueTask<object?> Next(EndpointFilterInvocationContext _)
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok("ok"));
        }

        // Act
        var result = await filter.InvokeAsync(context, Next);

        // Assert
        Assert.False(nextCalled);
        var httpResult = (IStatusCodeHttpResult)result!;
        Assert.Equal(StatusCodes.Status409Conflict, httpResult.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenHandlerReturnsNonSuccess_ShouldReleaseAndNotComplete()
    {
        // Arrange
        _store.TryReserveAsync(default!, default, default)
            .ReturnsForAnyArgs(IdempotencyEntry.Reserved);
        var filter = BuildFilter();
        var context = BuildContext("POST");

        static ValueTask<object?> Next(EndpointFilterInvocationContext _)
            => ValueTask.FromResult<object?>(Results.NotFound());

        // Act
        await filter.InvokeAsync(context, Next);

        // Assert
        await _store.ReceivedWithAnyArgs(1).ReleaseAsync(default!, default);
        await _store.DidNotReceiveWithAnyArgs().CompleteAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task InvokeAsync_WhenHandlerThrows_ShouldReleaseAndRethrow()
    {
        // Arrange
        _store.TryReserveAsync(default!, default, default)
            .ReturnsForAnyArgs(IdempotencyEntry.Reserved);
        var filter = BuildFilter();
        var context = BuildContext("POST");

        static ValueTask<object?> Next(EndpointFilterInvocationContext _)
            => throw new InvalidOperationException("boom");

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => filter.InvokeAsync(context, Next).AsTask()
        );
        await _store.ReceivedWithAnyArgs(1).ReleaseAsync(default!, default);
        await _store.DidNotReceiveWithAnyArgs().CompleteAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task InvokeAsync_WhenReserved_ShouldUseKeySuffixFactoryInKey()
    {
        // Arrange
        var capturedKey = string.Empty;
        _store.TryReserveAsync(default!, default, default)
            .ReturnsForAnyArgs(call =>
            {
                capturedKey = call.ArgAt<string>(0);
                return IdempotencyEntry.Reserved;
            });
        var filter = BuildFilter();
        var context = BuildContext("POST");

        // Act
        await filter.InvokeAsync(context, Ok);

        // Assert
        Assert.EndsWith(KeySuffix, capturedKey);
    }

    [Fact]
    public async Task InvokeAsync_WhenHandlerReturnsNonResult_ShouldReleaseAndThrow()
    {
        // Arrange
        _store.TryReserveAsync(default!, default, default)
            .ReturnsForAnyArgs(IdempotencyEntry.Reserved);
        var filter = BuildFilter();
        var context = BuildContext("POST");

        static ValueTask<object?> Next(EndpointFilterInvocationContext _)
            => ValueTask.FromResult<object?>("not an IResult");

        // Act
        var exception = await Record.ExceptionAsync(() => filter.InvokeAsync(context, Next).AsTask());

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
        await _store.ReceivedWithAnyArgs(1).ReleaseAsync(default!, default);
        await _store.DidNotReceiveWithAnyArgs().CompleteAsync(default!, default!, default, default);
    }

    private static ValueTask<object?> Ok(EndpointFilterInvocationContext _)
        => ValueTask.FromResult<object?>(Results.Ok("ok"));

    private static TestContext BuildContext(string method)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        httpContext.Request.Path = "/api/test";
        return new(httpContext);
    }

    private sealed class TestContext(HttpContext httpContext) : EndpointFilterInvocationContext
    {
        public override HttpContext HttpContext { get; } = httpContext;
        public override IList<object?> Arguments { get; } = [];

        public override TArgument GetArgument<TArgument>(int index)
            => (TArgument)Arguments[index]!;
    }
}
