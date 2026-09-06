using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;
using Microsoft.AspNetCore.MinimalApis.ApiSwagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi;
using NSubstitute;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiSwagger;

public sealed class ApiSummaryOperationFilterTests
{
    private readonly ApiSummaryOperationFilter _filter = new();

    [Fact]
    public void Apply_WhenNoSummaryTypeInAssembly_ShouldNotModifyOperation()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var context = BuildContext(typeof(NoSummaryEndpoint).GetMethod(nameof(NoSummaryEndpoint.Handle))!);

        // Act
        _filter.Apply(operation, context);

        // Assert
        Assert.Null(operation.Summary);
        Assert.Null(operation.Description);
    }

    [Fact]
    public void Apply_WhenSummaryTypeExists_ShouldApplySummaryToOperation()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var context = BuildContext(typeof(WithSummaryEndpoint).GetMethod(nameof(WithSummaryEndpoint.Handle))!);

        // Act
        _filter.Apply(operation, context);

        // Assert
        Assert.Equal("Test summary", operation.Summary);
        Assert.Equal("Test description", operation.Description);
    }

    private static OperationFilterContext BuildContext(MethodInfo method)
        => new(new ApiDescription(), Substitute.For<ISchemaGenerator>(), new SchemaRepository("v1"), new OpenApiDocument(), method);

    public sealed class NoSummaryEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
            => throw new NotImplementedException();

        public static void Handle()
        {
        }
    }

    public sealed class WithSummaryEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
            => throw new NotImplementedException();

        public static void Handle()
        {
        }
    }

    public sealed class WithSummaryEndpointSummary : ApiSummary<WithSummaryEndpoint>
    {
        public WithSummaryEndpointSummary()
        {
            Summary = "Test summary";
            Description = "Test description";
        }
    }
}
