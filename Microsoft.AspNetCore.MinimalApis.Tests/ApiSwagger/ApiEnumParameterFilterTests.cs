using Microsoft.AspNetCore.MinimalApis.ApiSwagger;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.TestHost;
using Microsoft.OpenApi;
using NSubstitute;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Net;
using System.Reflection;
using System.Text.Json.Nodes;
using Xunit;

namespace Microsoft.AspNetCore.MinimalApis.Tests.ApiSwagger;

public sealed class ApiEnumParameterFilterTests
{
    private readonly ApiEnumParameterFilter _filter = new();

    [Fact]
    public void Apply_WhenApiEnumQueryParameter_ShouldExposeStringEnumSchema()
    {
        // Arrange
        var operation = OperationWith("dummyEnum", ParameterLocation.Query);
        var context = BuildContext(new ApiParameterDescription
        {
            Name = "dummyEnum",
            Type = typeof(ApiEnum<DummyEnum>)
        });

        // Act
        _filter.Apply(operation, context);

        // Assert
        var schema = Assert.IsType<OpenApiSchema>(Assert.Single(operation.Parameters!).Schema);
        Assert.Equal(JsonSchemaType.String, schema.Type);
        Assert.Equal(["ValueOne", "ValueTwo"], schema.Enum!.Select(node => node.GetValue<string>()));
    }

    [Fact]
    public void Apply_WhenApiEnumPathParameter_ShouldExposeStringEnumSchema()
    {
        // Arrange
        var operation = OperationWith("dummyEnum", ParameterLocation.Path);
        var context = BuildContext(new ApiParameterDescription
        {
            Name = "dummyEnum",
            Type = typeof(ApiEnum<DummyEnum>)
        });

        // Act
        _filter.Apply(operation, context);

        // Assert
        var schema = Assert.IsType<OpenApiSchema>(Assert.Single(operation.Parameters!).Schema);
        Assert.Equal(["ValueOne", "ValueTwo"], schema.Enum!.Select(node => node.GetValue<string>()));
    }

    [Fact]
    public void Apply_WhenNoExampleOrDefault_ShouldDefaultToFirstMemberName()
    {
        // Arrange
        var operation = OperationWith("dummyEnum", ParameterLocation.Query);
        var context = BuildContext(new ApiParameterDescription
        {
            Name = "dummyEnum",
            Type = typeof(ApiEnum<DummyEnum>)
        });

        // Act
        _filter.Apply(operation, context);

        // Assert
        var schema = Assert.IsType<OpenApiSchema>(Assert.Single(operation.Parameters!).Schema);
        Assert.Equal("ValueOne", schema.Default!.GetValue<string>());
    }

    [Fact]
    public void Apply_WhenExampleIsWrapperObject_ShouldResolveDefaultFromObjectExample()
    {
        // Arrange
        var example = new JsonObject { ["Value"] = JsonValue.Create("ValueTwo") };
        var operation = OperationWith("dummyEnum", ParameterLocation.Query, example: example);
        var context = BuildContext(new ApiParameterDescription
        {
            Name = "dummyEnum",
            Type = typeof(ApiEnum<DummyEnum>)
        });

        // Act
        _filter.Apply(operation, context);

        // Assert
        var schema = Assert.IsType<OpenApiSchema>(Assert.Single(operation.Parameters!).Schema);
        Assert.Equal("ValueTwo", schema.Default!.GetValue<string>());
    }

    [Fact]
    public void Apply_WhenExampleIsString_ShouldResolveDefaultFromStringExample()
    {
        // Arrange
        var operation = OperationWith("dummyEnum", ParameterLocation.Query, example: JsonValue.Create("ValueTwo"));
        var context = BuildContext(new ApiParameterDescription
        {
            Name = "dummyEnum",
            Type = typeof(ApiEnum<DummyEnum>)
        });

        // Act
        _filter.Apply(operation, context);

        // Assert
        var schema = Assert.IsType<OpenApiSchema>(Assert.Single(operation.Parameters!).Schema);
        Assert.Equal("ValueTwo", schema.Default!.GetValue<string>());
    }

    [Fact]
    public void Apply_WhenSchemaHasValidDefault_ShouldRetainSchemaDefault()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Default = JsonValue.Create("ValueTwo")
        };
        var operation = OperationWith("dummyEnum", ParameterLocation.Query, schema);
        var context = BuildContext(new ApiParameterDescription
        {
            Name = "dummyEnum",
            Type = typeof(ApiEnum<DummyEnum>)
        });

        // Act
        _filter.Apply(operation, context);

        // Assert
        var applied = Assert.IsType<OpenApiSchema>(Assert.Single(operation.Parameters!).Schema);
        Assert.Equal("ValueTwo", applied.Default!.GetValue<string>());
    }

    [Fact]
    public void Apply_WhenExampleNotValidMemberName_ShouldFallBackToFirstMemberName()
    {
        // Arrange
        var operation = OperationWith("dummyEnum", ParameterLocation.Query, example: JsonValue.Create("banana"));
        var context = BuildContext(new ApiParameterDescription
        {
            Name = "dummyEnum",
            Type = typeof(ApiEnum<DummyEnum>)
        });

        // Act
        _filter.Apply(operation, context);

        // Assert
        var schema = Assert.IsType<OpenApiSchema>(Assert.Single(operation.Parameters!).Schema);
        Assert.Equal("ValueOne", schema.Default!.GetValue<string>());
    }

    [Fact]
    public void Apply_WhenApiEnumParameter_ShouldClearExampleAndContent()
    {
        // Arrange
        var operation = OperationWith("dummyEnum", ParameterLocation.Query, example: JsonValue.Create("ValueTwo"));
        var parameter = (OpenApiParameter)operation.Parameters![0];
        parameter.Content = new Dictionary<string, OpenApiMediaType> { ["application/json"] = new() };
        var context = BuildContext(new ApiParameterDescription
        {
            Name = "dummyEnum",
            Type = typeof(ApiEnum<DummyEnum>)
        });

        // Act
        _filter.Apply(operation, context);

        // Assert
        Assert.Null(parameter.Example);
        Assert.Null(parameter.Content);
    }

    [Fact]
    public void Apply_WhenPlainEnumParameter_ShouldLeaveSchemaUntouched()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = JsonSchemaType.String };
        var operation = OperationWith("dummyEnum", ParameterLocation.Query, schema);
        var context = BuildContext(new ApiParameterDescription
        {
            Name = "dummyEnum",
            Type = typeof(DummyEnum)
        });

        // Act
        _filter.Apply(operation, context);

        // Assert
        Assert.Same(schema, Assert.Single(operation.Parameters!).Schema);
    }

    [Fact]
    public void Apply_WhenNonEnumParameter_ShouldLeaveSchemaUntouched()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = JsonSchemaType.String };
        var operation = OperationWith("search", ParameterLocation.Query, schema);
        var context = BuildContext(new ApiParameterDescription
        {
            Name = "search",
            Type = typeof(string)
        });

        // Act
        _filter.Apply(operation, context);

        // Assert
        Assert.Same(schema, Assert.Single(operation.Parameters!).Schema);
    }

    [Fact]
    public void Apply_WhenNoParameters_ShouldNotThrow()
    {
        // Arrange
        var operation = new OpenApiOperation();
        var context = BuildContext();

        // Act
        _filter.Apply(operation, context);

        // Assert
        Assert.Null(operation.Parameters);
    }

    [Theory]
    [InlineData("ValueOne", HttpStatusCode.OK, "ValueOne")]
    [InlineData("valueone", HttpStatusCode.OK, "ValueOne")]
    [InlineData("VALUETWO", HttpStatusCode.OK, "ValueTwo")]
    [InlineData("1", HttpStatusCode.OK, "ValueTwo")]
    [InlineData("99", HttpStatusCode.OK, "99")]
    [InlineData("banana", HttpStatusCode.BadRequest, null)]
    public async Task ApiEnumBinding_IsCaseInsensitiveAndNumeric(
        string value,
        HttpStatusCode expectedStatus,
        string? expectedBody)
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        app.MapGet("/bind", (ApiEnum<DummyEnum> dummyEnum) => Results.Text(((DummyEnum)dummyEnum).ToString()));
        await app.StartAsync();

        // Act
        var response = await app.GetTestClient().GetAsync($"/bind?dummyEnum={value}");

        // Assert
        Assert.Equal(expectedStatus, response.StatusCode);
        if (expectedBody is not null)
        {
            Assert.Equal(expectedBody, await response.Content.ReadAsStringAsync());
        }

        await app.StopAsync();
    }

    private static OpenApiOperation OperationWith(
        string name,
        ParameterLocation location,
        OpenApiSchema? schema = null,
        JsonNode? example = null)
        => new()
        {
            Parameters =
            [
                new OpenApiParameter
                {
                    Name = name,
                    In = location,
                    Schema = schema ?? new OpenApiSchema { Type = JsonSchemaType.String },
                    Example = example,
                }
            ]
        };

    private static OperationFilterContext BuildContext(params ApiParameterDescription[] parameterDescriptions)
    {
        var apiDescription = new ApiDescription();
        foreach (var description in parameterDescriptions)
        {
            apiDescription.ParameterDescriptions.Add(description);
        }

        var method = typeof(ApiEnumParameterFilterTests)
            .GetMethod(nameof(NoOp), BindingFlags.NonPublic | BindingFlags.Static)!;

        return new OperationFilterContext(
            apiDescription,
            Substitute.For<ISchemaGenerator>(),
            new SchemaRepository("v1"),
            new OpenApiDocument(),
            method);
    }

    private static void NoOp()
    {
    }
}
