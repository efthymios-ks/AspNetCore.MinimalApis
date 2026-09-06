using Microsoft.AspNetCore.MinimalApis.ApiSwagger;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using NSubstitute;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiSwagger;

public sealed class ApiParameterOperationFilterTests
{
    private readonly ApiParameterOperationFilter _filter = new();

    [Fact]
    public void Apply_WhenDeclaringTypeIsNull_ShouldSkip()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var methodInfo = Substitute.For<MethodInfo>();
        methodInfo.DeclaringType.Returns((Type?)null);
        var context = BuildContext(methodInfo);

        // Act
        _filter.Apply(operation, context);

        // Assert
        Assert.Null(operation.Parameters);
    }

    [Fact]
    public void Apply_WhenHeaderAttribute_ShouldAddHeaderParameter()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var context = BuildContext(typeof(WithHeaderEndpoint).GetMethod(nameof(WithHeaderEndpoint.Handle))!);

        // Act
        _filter.Apply(operation, context);

        // Assert
        Assert.NotNull(operation.Parameters);
        Assert.Contains(operation.Parameters, parameter
            => parameter.Name == "X-Custom-Header"
            && parameter.In == ParameterLocation.Header
        );
    }

    [Fact]
    public void Apply_WhenQueryAttribute_ShouldAddQueryParameter()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var context = BuildContext(typeof(WithQueryEndpoint).GetMethod(nameof(WithQueryEndpoint.Handle))!);

        // Act
        _filter.Apply(operation, context);

        // Assert
        Assert.NotNull(operation.Parameters);
        Assert.Contains(operation.Parameters, parameter
            => parameter.Name == "search"
            && parameter.In == ParameterLocation.Query
        );
    }

    [Fact]
    public void Apply_WhenHeaderAttributeHasDefault_ShouldSetDefault()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var context = BuildContext(typeof(WithDefaultHeaderEndpoint).GetMethod(nameof(WithDefaultHeaderEndpoint.Handle))!);

        // Act
        _filter.Apply(operation, context);

        // Assert
        Assert.NotNull(operation.Parameters);
        var param = Assert.Single(operation.Parameters);
        Assert.NotNull(param.Schema?.Default);
    }

    [Fact]
    public void Apply_WhenHeaderAttributeIsRequired_ShouldMarkRequired()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var context = BuildContext(typeof(WithRequiredHeaderEndpoint).GetMethod(nameof(WithRequiredHeaderEndpoint.Handle))!);

        // Act
        _filter.Apply(operation, context);

        // Assert
        Assert.NotNull(operation.Parameters);
        var param = Assert.Single(operation.Parameters);
        Assert.True(param.Required);
    }

    [Fact]
    public void Apply_WhenDropdownAttributeValid_ShouldAddEnumParameter()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var context = BuildContext(typeof(WithDropdownEndpoint).GetMethod(nameof(WithDropdownEndpoint.Handle))!);

        // Act
        _filter.Apply(operation, context);

        // Assert
        Assert.NotNull(operation.Parameters);
        var param = Assert.Single(operation.Parameters);
        Assert.NotNull(param.Schema?.Enum);
    }

    [Fact]
    public void Apply_WhenDropdownDefaultNotInValues_ShouldThrow()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var context = BuildContext(typeof(WithInvalidDefaultDropdownEndpoint).GetMethod(nameof(WithInvalidDefaultDropdownEndpoint.Handle))!);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _filter.Apply(operation, context));
    }

    [Fact]
    public void Apply_WhenDropdownDefaultNull_ShouldNotSetDefault()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var context = BuildContext(typeof(WithDropdownEndpoint).GetMethod(nameof(WithDropdownEndpoint.Handle))!);

        // Act
        _filter.Apply(operation, context);

        // Assert
        Assert.NotNull(operation.Parameters);
        var param = Assert.Single(operation.Parameters);
        Assert.Null(param.Schema?.Default);
    }

    [Fact]
    public void Apply_WhenMultipleHeaderAttributes_ShouldAddAllHeaders()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var context = BuildContext(typeof(WithMultipleHeadersEndpoint).GetMethod(nameof(WithMultipleHeadersEndpoint.Handle))!);

        // Act
        _filter.Apply(operation, context);

        // Assert
        Assert.NotNull(operation.Parameters);
        Assert.Equal(2, operation.Parameters.Count);
        Assert.Contains(operation.Parameters, parameter => parameter.Name == "X-Header-One");
        Assert.Contains(operation.Parameters, parameter => parameter.Name == "X-Header-Two");
    }

    [Fact]
    public void Apply_WhenHeaderAndQueryAttributes_ShouldAddBoth()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var context = BuildContext(typeof(WithHeaderAndQueryEndpoint).GetMethod(nameof(WithHeaderAndQueryEndpoint.Handle))!);

        // Act
        _filter.Apply(operation, context);

        // Assert
        Assert.NotNull(operation.Parameters);
        Assert.Equal(2, operation.Parameters.Count);
        Assert.Contains(operation.Parameters, parameter => parameter.In == ParameterLocation.Header);
        Assert.Contains(operation.Parameters, parameter => parameter.In == ParameterLocation.Query);
    }

    private static OperationFilterContext BuildContext(MethodInfo method)
        => new(new ApiDescription(), Substitute.For<ISchemaGenerator>(), new SchemaRepository("v1"), new OpenApiDocument(), method);

    [ApiHeader("X-Custom-Header")]
    private sealed class WithHeaderEndpoint
    {
        public static void Handle()
        {
        }
    }

    [ApiQuery("search")]
    private sealed class WithQueryEndpoint
    {
        public static void Handle()
        {
        }
    }

    [ApiHeader("X-Default", defaultValue: "my-default")]
    private sealed class WithDefaultHeaderEndpoint
    {
        public static void Handle()
        {
        }
    }

    [ApiHeader("X-Required", isRequired: true)]
    private sealed class WithRequiredHeaderEndpoint
    {
        public static void Handle()
        {
        }
    }

    [TestDropdownHeader("color", "red", "green", "blue")]
    private sealed class WithDropdownEndpoint
    {
        public static void Handle()
        {
        }
    }

    [TestInvalidDefaultDropdownHeader]
    private sealed class WithInvalidDefaultDropdownEndpoint
    {
        public static void Handle()
        {
        }
    }

    [ApiHeader("X-Header-One")]
    [ApiHeader("X-Header-Two")]
    private sealed class WithMultipleHeadersEndpoint
    {
        public static void Handle()
        {
        }
    }

    [ApiHeader("X-Mixed-Header")]
    [ApiQuery("mixed-query")]
    private sealed class WithHeaderAndQueryEndpoint
    {
        public static void Handle()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    private sealed class TestDropdownHeaderAttribute(string key, string v1, string v2, string v3)
        : ApiHeaderDropdownAttributeBase
    {
        public override string Key { get; } = key;
        public override IEnumerable<string> Values { get; } = [v1, v2, v3];
    }

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class TestInvalidDefaultDropdownHeaderAttribute : ApiHeaderDropdownAttributeBase
    {
        public override string Key { get; } = "color";
        public override string? DefaultValue { get; } = "purple";
        public override IEnumerable<string> Values { get; } = ["red", "green"];
    }
}
