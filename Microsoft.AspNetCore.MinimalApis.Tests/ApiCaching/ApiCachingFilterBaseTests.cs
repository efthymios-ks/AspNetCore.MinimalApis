using Microsoft.AspNetCore.MinimalApis.ApiCaching;
using Microsoft.AspNetCore.MinimalApis.ApiCaching.Local;
using Microsoft.AspNetCore.MinimalApis.Utilities;
using NSubstitute;
using System.Text.Json;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiCaching;

public sealed class ApiCachingFilterBaseTests
{
    [Fact]
    public async Task InvokeAsync_WhenPostRequest_ShouldReturn405()
    {
        // Arrange
        var filter = new TestCachingFilter();
        var context = BuildContext("POST");

        static ValueTask<object?> next(EndpointFilterInvocationContext _)
            => ValueTask.FromResult<object?>("ok");

        // Act
        var result = await filter.InvokeAsync(context, next);

        // Assert
        var httpResult = Assert.IsType<IStatusCodeHttpResult>(result, exactMatch: false);
        Assert.Equal(StatusCodes.Status405MethodNotAllowed, httpResult.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenGetRequest_WithCacheMiss_ShouldCallNextAndCacheResult()
    {
        // Arrange
        var filter = new TestCachingFilter();
        var context = BuildContext("GET");

        var nextCalled = false;
        ValueTask<object?> Next(EndpointFilterInvocationContext _)
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok("hello"));
        }

        // Act
        await filter.InvokeAsync(context, Next);

        // Assert
        Assert.True(nextCalled);
        Assert.True(filter.HasAnyCached());
    }

    [Fact]
    public async Task InvokeAsync_WhenGetRequest_WithCacheHit_ShouldReturnCachedResult()
    {
        // Arrange
        var filter = new TestCachingFilter();
        var context = BuildContext("GET", "/cached");
        var cacheKey = ApiCachingOptionsBase.GetKeyPrefix(context);
        filter.SeedCache(cacheKey, 200, "text/plain", "cached-body"u8.ToArray());

        var nextCalled = false;
        ValueTask<object?> Next(EndpointFilterInvocationContext _)
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>("fresh");
        }

        // Act
        await filter.InvokeAsync(context, Next);

        // Assert
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenGetRequest_WithCacheMiss_AndNullNextResult_ShouldReturnNull()
    {
        // Arrange
        var filter = new TestCachingFilter();
        var context = BuildContext("GET");

        static ValueTask<object?> Next(EndpointFilterInvocationContext _)
            => ValueTask.FromResult<object?>(null);

        // Act
        var result = await filter.InvokeAsync(context, Next);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task InvokeAsync_WhenGetRequest_WithCacheMiss_AndNon200Status_ShouldNotCache()
    {
        // Arrange
        var filter = new TestCachingFilter();
        var context = BuildContext("GET");

        static ValueTask<object?> next(EndpointFilterInvocationContext _)
            => ValueTask.FromResult<object?>(Results.NotFound());

        // Act
        await filter.InvokeAsync(context, next);

        // Assert
        Assert.False(filter.HasAnyCached());
    }

    [Fact]
    public async Task InvokeAsync_WhenResponseHasStarted_ShouldBypassCacheReadAndCallNext()
    {
        // Arrange
        var filter = new TestCachingFilter();
        filter.SeedCache("ApiCaching:en:GET:/test::suffix", 200, "text/plain", "cached"u8.ToArray());

        var httpContext = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();
        var response = Substitute.For<HttpResponse>();
        request.Method.Returns(HttpMethods.Get);
        request.Path.Returns(new PathString("/test"));
        request.QueryString.Returns(new QueryString());
        response.HasStarted.Returns(true);
        httpContext.Request.Returns(request);
        httpContext.Response.Returns(response);
        httpContext.RequestAborted.Returns(CancellationToken.None);
        var context = new TestContext(httpContext);

        var nextCalled = false;
        ValueTask<object?> Next(EndpointFilterInvocationContext _)
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok("fresh"));
        }

        // Act
        await filter.InvokeAsync(context, Next);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenGetRequest_WithCacheMiss_AndNonResultNextResult_ShouldThrow()
    {
        // Arrange
        var filter = new TestCachingFilter();
        var context = BuildContext("GET");

        static ValueTask<object?> Next(EndpointFilterInvocationContext _)
            => ValueTask.FromResult<object?>("not an IResult");

        // Act
        var exception = await Record.ExceptionAsync(() => filter.InvokeAsync(context, Next).AsTask());

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    private static TestContext BuildContext(string method, string path = "/test")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        httpContext.Request.Path = path;
        return new(httpContext);
    }

    private sealed class TestContext(HttpContext httpContext) : EndpointFilterInvocationContext
    {
        public override HttpContext HttpContext { get; } = httpContext;
        public override IList<object?> Arguments { get; } = [];
        public override TArgument GetArgument<TArgument>(int index)
            => (TArgument)Arguments[index]!;
    }

    private sealed class TestCachingFilter() : ApiCachingFilterBase(new LocalApiCachingOptions())
    {
        private readonly Dictionary<string, byte[]> _store = [];

        public void SeedCache(
            string key,
            int statusCode,
            string? contentType,
            byte[] body
        )
        {
            var response = new CachedResponse(statusCode, contentType, body);
            _store[key] = JsonSerializer.SerializeToUtf8Bytes(response);
        }

        public bool HasAnyCached()
            => _store.Count > 0;

        public override Task<byte[]?> GetFromCacheAsync(string cacheKey, CancellationToken cancellationToken)
            => Task.FromResult(_store.TryGetValue(cacheKey, out var value) ? value : null);

        public override Task AddToCacheAsync(
            string cacheKey,
            byte[] value,
            TimeSpan expiration,
            CancellationToken cancellationToken
        )
        {
            _store[cacheKey] = value;
            return Task.CompletedTask;
        }
    }
}
