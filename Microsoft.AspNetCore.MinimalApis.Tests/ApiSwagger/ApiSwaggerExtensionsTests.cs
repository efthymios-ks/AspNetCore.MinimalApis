using Microsoft.AspNetCore.MinimalApis.ApiSwagger;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiSwagger;

public sealed class ApiSwaggerExtensionsTests
{
    [Fact]
    public void ConfigureApiEndpoints_WhenNull_ShouldThrow()
    {
        // Arrange
        SwaggerGenOptions options = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(options.ConfigureApiEndpoints);
    }

    [Fact]
    public void ConfigureApiEndpoints_WhenCalled_ShouldAddSummaryFilter()
    {
        // Arrange
        var options = BuildOptions();

        // Act
        options.ConfigureApiEndpoints();

        // Assert
        Assert.Contains(options.OperationFilterDescriptors, descriptor
            => descriptor.Type == typeof(ApiSummaryOperationFilter)
        );
    }

    [Fact]
    public void ConfigureApiEndpoints_WhenCalled_ShouldAddParameterFilter()
    {
        // Arrange
        var options = BuildOptions();

        // Act
        options.ConfigureApiEndpoints();

        // Assert
        Assert.Contains(options.OperationFilterDescriptors, descriptor
            => descriptor.Type == typeof(ApiParameterOperationFilter)
        );
    }

    [Fact]
    public void ConfigureApiEndpoints_WhenCalled_ShouldAddVersionDeprecatedFilter()
    {
        // Arrange
        var options = BuildOptions();

        // Act
        options.ConfigureApiEndpoints();

        // Assert
        Assert.Contains(options.OperationFilterDescriptors, descriptor
            => descriptor.Type == typeof(ApiVersionDeprecatedOperationFilter)
        );
    }

    [Fact]
    public void ConfigureApiEndpoints_WhenCalled_ShouldAddApiEnumFilter()
    {
        // Arrange
        var options = BuildOptions();

        // Act
        options.ConfigureApiEndpoints();

        // Assert
        Assert.Contains(options.OperationFilterDescriptors, descriptor
            => descriptor.Type == typeof(ApiEnumParameterFilter)
        );
    }

    private static SwaggerGenOptions BuildOptions()
    {
        SwaggerGenOptions captured = new();
        new ServiceCollection()
            .AddSwaggerGen(option => captured = option)
            .BuildServiceProvider();
        return captured;
    }
}
