using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;
using Microsoft.AspNetCore.MinimalApis.ApiSwagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi;
using System.Net.Mime;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiSwagger;

public sealed class ApiSummaryTests
{
    [Fact]
    public void Apply_WhenSummarySet_ShouldSetOperationSummary()
    {
        // Arrange
        var summary = new TestEndpointSummary
        {
            Summary = "My summary"
        };
        var operation = new OpenApiOperation();

        // Act
        summary.Apply(operation);

        // Assert
        Assert.Equal("My summary", operation.Summary);
    }

    [Fact]
    public void Apply_WhenDescriptionSet_ShouldSetOperationDescription()
    {
        // Arrange
        var summary = new TestEndpointSummary
        {
            Description = "My description"
        };
        var operation = new OpenApiOperation();

        // Act
        summary.Apply(operation);

        // Assert
        Assert.Equal("My description", operation.Description);
    }

    [Fact]
    public void Apply_WhenNoBodyExamples_ShouldNotModifyContent()
    {
        // Arrange
        var summary = new TestEndpointSummary();
        var operation = new OpenApiOperation();

        // Act
        summary.Apply(operation);

        // Assert
        Assert.Null(operation.RequestBody);
    }

    [Fact]
    public void Apply_WhenBodyExamplesExistButNoJsonContent_ShouldNotAddExamples()
    {
        // Arrange
        var summary = new TestEndpointSummaryWithBodyExample();
        var mediaType = new OpenApiMediaType();
        var operation = new OpenApiOperation
        {
            RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    [MediaTypeNames.Text.Plain] = mediaType
                }
            }
        };

        // Act
        summary.Apply(operation);

        // Assert
        Assert.Null(mediaType.Examples);
    }

    [Fact]
    public void Apply_WhenBodyExamplesExistWithJsonContent_ShouldAddExamples()
    {
        // Arrange
        var summary = new TestEndpointSummaryWithBodyExample();
        var mediaType = new OpenApiMediaType();
        var operation = new OpenApiOperation
        {
            RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    [MediaTypeNames.Application.Json] = mediaType
                }
            }
        };

        // Act
        summary.Apply(operation);

        // Assert
        Assert.NotNull(mediaType.Examples);
        Assert.Contains("ex1", mediaType.Examples.Keys);
    }

    [Fact]
    public void Apply_WhenResponseExampleAndJsonContent_ShouldAddExample()
    {
        // Arrange
        var summary = new TestEndpointSummaryWithResponseExample();
        var mediaType = new OpenApiMediaType();
        var operation = new OpenApiOperation
        {
            Responses = new OpenApiResponses
            {
                ["200"] = new OpenApiResponse
                {
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        [MediaTypeNames.Application.Json] = mediaType
                    }
                }
            }
        };

        // Act
        summary.Apply(operation);

        // Assert
        Assert.NotNull(mediaType.Examples);
        Assert.Contains("ok", mediaType.Examples.Keys);
    }

    [Fact]
    public void Apply_WhenResponseHasNoContent_ShouldCreateJsonContentWithExample()
    {
        // Arrange
        var summary = new TestEndpointSummaryWithResponseExample();
        var response = new OpenApiResponse();
        var operation = new OpenApiOperation
        {
            Responses = new OpenApiResponses
            {
                ["200"] = response
            }
        };

        // Act
        summary.Apply(operation);

        // Assert
        Assert.NotNull(response.Content);
        Assert.True(response.Content.ContainsKey(MediaTypeNames.Application.Json));
    }

    [Fact]
    public void Apply_WhenParameterExamplesFromDto_ShouldSetMatchingParameterExamples()
    {
        // Arrange
        var summary = new TestEndpointSummaryWithDtoParameters();
        var operation = new OpenApiOperation
        {
            Parameters =
            [
                new OpenApiParameter { Name = "categoryId", In = ParameterLocation.Query },
                new OpenApiParameter { Name = "search", In = ParameterLocation.Query }
            ]
        };

        // Act
        summary.Apply(operation);

        // Assert — PascalCase DTO properties match camelCased parameters (case-insensitive)
        var category = operation.Parameters.OfType<OpenApiParameter>().Single(parameter => parameter.Name == "categoryId");
        var search = operation.Parameters.OfType<OpenApiParameter>().Single(parameter => parameter.Name == "search");
        Assert.NotNull(category.Example);
        Assert.NotNull(search.Example);
    }

    [Fact]
    public void AddParameterExamples_WhenNull_ShouldThrow()
    {
        // Arrange
        var summary = new TestEndpointSummaryWithDtoParameters();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => summary.AddParameterExamples<object>(null!));
    }

    [Fact]
    public async Task ConfigureAsync_WhenNotOverridden_ShouldReturnCompletedTask()
    {
        // Arrange
        var summary = new TestEndpointSummary();

        // Act
        var exception = await Record.ExceptionAsync(
            () => summary.ConfigureAsync(null!, null!, null!)
        );

        // Assert
        Assert.Null(exception);
    }

    private sealed class TestEndpoint : ApiEndpoint
    {
        public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
            => throw new NotImplementedException();
    }

    private sealed class TestEndpointSummary : ApiSummary<TestEndpoint>;

    private sealed class TestEndpointSummaryWithBodyExample : ApiSummary<TestEndpoint>
    {
        public TestEndpointSummaryWithBodyExample()
            => AddBodyExample("ex1", new { Name = "test" });
    }

    private sealed class TestEndpointSummaryWithResponseExample : ApiSummary<TestEndpoint>
    {
        public TestEndpointSummaryWithResponseExample()
            => AddResponseExample(200, "ok", new { Name = "test" });
    }

    private sealed class TestEndpointSummaryWithDtoParameters : ApiSummary<TestEndpoint>
    {
        public TestEndpointSummaryWithDtoParameters()
            => AddParameterExamples(new SampleParameters { CategoryId = 5, Search = "Smartphone" });
    }

    private sealed class SampleParameters
    {
        public int CategoryId { get; set; }
        public string? Search { get; set; }
    }
}
