using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiEndpoints;

public sealed class ApiEndpointsOptionsTests
{
    [Fact]
    public void AddGlobalEndpointFilter_WithType_WhenNull_ShouldThrow()
    {
        // Arrange
        var options = new ApiEndpointsOptions();

        // Act
        ApiEndpointsOptions Act()
            => options.AddGlobalEndpointFilter((Type)null!);

        // Assert
        Assert.Throws<ArgumentNullException>((Func<ApiEndpointsOptions>)Act);
    }

    [Fact]
    public void AddGlobalEndpointFilter_WithType_ShouldAddToFilterTypes()
    {
        // Arrange
        var options = new ApiEndpointsOptions();

        // Act
        options.AddGlobalEndpointFilter<TestFilter>();

        // Assert
        Assert.Contains(typeof(TestFilter), options.EndpointFilterTypes);
    }

    [Fact]
    public void AddGlobalEndpointFilter_WithType_ShouldReturnSameOptions()
    {
        // Arrange
        var options = new ApiEndpointsOptions();

        // Act
        var returned = options.AddGlobalEndpointFilter<TestFilter>();

        // Assert
        Assert.Same(options, returned);
    }

    [Fact]
    public void AddGlobalEndpointFilter_Generic_ShouldAddToFilterTypes()
    {
        // Arrange
        var options = new ApiEndpointsOptions();

        // Act
        options.AddGlobalEndpointFilter<TestFilter>();

        // Assert
        Assert.Contains(typeof(TestFilter), options.EndpointFilterTypes);
    }

    [Fact]
    public void AddGlobalEndpointFilter_Generic_ShouldReturnSameOptions()
    {
        // Arrange
        var options = new ApiEndpointsOptions();

        // Act
        var returned = options.AddGlobalEndpointFilter<TestFilter>();

        // Assert
        Assert.Same(options, returned);
    }

    [Fact]
    public void AddGlobalEndpointFilter_WithFactory_WhenNull_ShouldThrow()
    {
        // Arrange
        var options = new ApiEndpointsOptions();

        // Act
        ApiEndpointsOptions Act()
            => options.AddGlobalEndpointFilter((Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate>)null!);

        // Assert
        Assert.Throws<ArgumentNullException>((Func<ApiEndpointsOptions>)Act);
    }

    [Fact]
    public void AddGlobalEndpointFilter_WithFactory_ShouldAddToFilterFactories()
    {
        // Arrange
        var options = new ApiEndpointsOptions();
        static EndpointFilterDelegate factory(EndpointFilterFactoryContext factoryContext, EndpointFilterDelegate next) => next;

        // Act
        options.AddGlobalEndpointFilter(factory);

        // Assert
        Assert.Contains(factory, options.EndpointFilterFactories);
    }

    [Fact]
    public void AddGlobalEndpointFilter_WithFactory_ShouldReturnSameOptions()
    {
        // Arrange
        var options = new ApiEndpointsOptions();
        static EndpointFilterDelegate factory(EndpointFilterFactoryContext factoryContext, EndpointFilterDelegate next) => next;

        // Act
        var returned = options.AddGlobalEndpointFilter(factory);

        // Assert
        Assert.Same(options, returned);
    }

    [Fact]
    public void AddGlobalEndpointFilter_WithNonGenericType_ShouldAddToFilterTypes()
    {
        // Arrange
        var options = new ApiEndpointsOptions();

        // Act
        options.AddGlobalEndpointFilter<TestFilter>();

        // Assert
        Assert.Contains(typeof(TestFilter), options.EndpointFilterTypes);
    }

    [Fact]
    public void AddGlobalEndpointFilter_WithNonGenericType_ShouldReturnSameOptions()
    {
        // Arrange
        var options = new ApiEndpointsOptions();

        // Act
        var returned = options.AddGlobalEndpointFilter<TestFilter>();

        // Assert
        Assert.Same(options, returned);
    }

    private sealed class TestFilter : IEndpointFilter
    {
        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
            => next(context);
    }
}
