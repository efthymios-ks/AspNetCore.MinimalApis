using Asp.Versioning;
using Microsoft.AspNetCore.MinimalApis.ApiSwagger;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using NSubstitute;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiVersions;

public sealed class ApiVersionDeprecatedOperationFilterTests
{
    private readonly ApiVersionDeprecatedOperationFilter _filter = new();

    [Fact]
    public void Apply_WhenNotDeprecated_ShouldNotSetDeprecated()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var context = BuildContext(typeof(ActiveEndpoint).GetMethod(nameof(ActiveEndpoint.Handle))!);

        // Act
        _filter.Apply(operation, context);

        // Assert
        Assert.False(operation.Deprecated);
    }

    [Fact]
    public void Apply_WhenDeprecated_ShouldSetOperationDeprecated()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var context = BuildContext(typeof(DeprecatedEndpoint).GetMethod(nameof(DeprecatedEndpoint.Handle))!);

        // Act
        _filter.Apply(operation, context);

        // Assert
        Assert.True(operation.Deprecated);
    }

    private static OperationFilterContext BuildContext(MethodInfo method)
        => new(new ApiDescription(), Substitute.For<ISchemaGenerator>(), new SchemaRepository("v1"), new OpenApiDocument(), method);

    [ApiVersion("1.0")]
    private sealed class ActiveEndpoint
    {
        public static void Handle()
        {
        }
    }

    [ApiVersion("1.0", Deprecated = true)]
    private sealed class DeprecatedEndpoint
    {
        public static void Handle()
        {
        }
    }
}
