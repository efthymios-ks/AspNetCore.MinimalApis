using Microsoft.AspNetCore.MinimalApis.ApiCaching;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiCaching;

public sealed class ApiCachingOptionsBaseTests
{
    [Fact]
    public void GetKeyPrefix_WhenCalled_ShouldIncludeMethod()
    {
        // Arrange
        var context = BuildContext("GET", "/api/users", "");

        // Act
        var key = ApiCachingOptionsBase.GetKeyPrefix(context);

        // Assert
        Assert.Contains("GET", key);
    }

    [Fact]
    public void GetKeyPrefix_WhenCalled_ShouldIncludePath()
    {
        // Arrange
        var context = BuildContext("GET", "/api/users", "");

        // Act
        var key = ApiCachingOptionsBase.GetKeyPrefix(context);

        // Assert
        Assert.Contains("/api/users", key);
    }

    [Fact]
    public void GetKeyPrefix_WhenCalled_ShouldIncludeQueryString()
    {
        // Arrange
        var context = BuildContext("GET", "/api/users", "?page=2");

        // Act
        var key = ApiCachingOptionsBase.GetKeyPrefix(context);

        // Assert
        Assert.Contains("page=2", key);
    }

    [Fact]
    public void GetKeyPrefix_WhenCalled_ShouldIncludeCulture()
    {
        // Arrange
        var context = BuildContext("GET", "/api/items", "");
        var expected = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        // Act
        var key = ApiCachingOptionsBase.GetKeyPrefix(context);

        // Assert
        Assert.Contains(expected, key);
    }

    [Fact]
    public void GetKeyPrefix_WhenCalled_ShouldStartWithApiCachingPrefix()
    {
        // Arrange
        var context = BuildContext("GET", "/", "");

        // Act
        var key = ApiCachingOptionsBase.GetKeyPrefix(context);

        // Assert
        Assert.StartsWith("ApiCaching:", key);
    }

    [Fact]
    public void CacheDuration_ShouldDefaultToThirtyMinutes()
    {
        // Arrange
        var options = new ConcreteOptions();

        // Act
        var duration = options.CacheDuration;

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(30), duration);
    }

    [Fact]
    public void KeySuffixFactory_WhenDefault_ShouldReturnEmptyString()
    {
        // Arrange
        var options = new ConcreteOptions();
        var context = BuildContext("GET", "/", "");

        // Act
        var suffix = options.KeySuffixFactory(context);

        // Assert
        Assert.Equal(string.Empty, suffix);
    }

    private sealed class ConcreteOptions : ApiCachingOptionsBase;

    private static TestContext BuildContext(
        string method,
        string path,
        string queryString
    )
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        httpContext.Request.Path = new(path);
        httpContext.Request.QueryString = new QueryString(queryString.Length > 0 ? queryString : string.Empty);
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
