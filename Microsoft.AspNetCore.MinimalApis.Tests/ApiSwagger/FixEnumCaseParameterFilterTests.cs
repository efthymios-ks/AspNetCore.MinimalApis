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

public sealed class FixEnumCaseParameterFilterTests
{
    private readonly FixEnumCaseParameterFilter _filter = new();

    [Fact]
    public void Apply_WhenEnumQueryParameter_ShouldExposeExactPascalCaseNames()
    {
        // Arrange
        var operation = OperationWith("dummyEnum", ParameterLocation.Query);
        var context = BuildContext(new ApiParameterDescription
        {
            Name = "dummyEnum",
            Type = typeof(DummyEnum)
        });

        // Act
        _filter.Apply(operation, context);

        // Assert
        var schema = Assert.IsType<OpenApiSchema>(Assert.Single(operation.Parameters!).Schema);
        Assert.Equal(JsonSchemaType.String, schema.Type);
        Assert.Equal(["ValueOne", "ValueTwo"], schema.Enum!.Select(node => node.GetValue<string>()));
    }

    [Fact]
    public void Apply_WhenEnumPathParameter_ShouldExposeExactPascalCaseNames()
    {
        // Arrange
        var operation = OperationWith("dummyEnum", ParameterLocation.Path);
        var context = BuildContext(new ApiParameterDescription
        {
            Name = "dummyEnum",
            Type = typeof(DummyEnum)
        });

        // Act
        _filter.Apply(operation, context);

        // Assert
        var schema = Assert.IsType<OpenApiSchema>(Assert.Single(operation.Parameters!).Schema);
        Assert.Equal(["ValueOne", "ValueTwo"], schema.Enum!.Select(node => node.GetValue<string>()));
    }

    [Fact]
    public void Apply_WhenNullableEnumQueryParameter_ShouldExposeExactPascalCaseNames()
    {
        // Arrange
        var operation = OperationWith("dummyEnum", ParameterLocation.Query);
        var context = BuildContext(new ApiParameterDescription
        {
            Name = "dummyEnum",
            Type = typeof(DummyEnum?)
        });

        // Act
        _filter.Apply(operation, context);

        // Assert
        var schema = Assert.IsType<OpenApiSchema>(Assert.Single(operation.Parameters!).Schema);
        Assert.Equal(2, schema.Enum!.Count);
    }

    [Fact]
    public void Apply_WhenNoDefaultOrExample_ShouldDefaultToFirstMemberName()
    {
        // Arrange
        var operation = OperationWith("dummyEnum", ParameterLocation.Query);
        var context = BuildContext(new ApiParameterDescription
        {
            Name = "dummyEnum",
            Type = typeof(DummyEnum)
        });

        // Act
        _filter.Apply(operation, context);

        // Assert
        var schema = Assert.IsType<OpenApiSchema>(Assert.Single(operation.Parameters!).Schema);
        Assert.Equal("ValueOne", schema.Default!.GetValue<string>());
    }

    [Fact]
    public void Apply_WhenSchemaHasDefault_ShouldRetainIt()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Default = JsonValue.Create("ValueTwo") };
        var operation = OperationWith("dummyEnum", ParameterLocation.Query, schema);
        var context = BuildContext(new ApiParameterDescription
        {
            Name = "dummyEnum",
            Type = typeof(DummyEnum)
        });

        // Act
        _filter.Apply(operation, context);

        // Assert
        var applied = Assert.IsType<OpenApiSchema>(Assert.Single(operation.Parameters!).Schema);
        Assert.Equal("ValueTwo", applied.Default!.GetValue<string>());
    }

    [Fact]
    public void Apply_WhenNonEnumQueryParameter_ShouldLeaveSchemaUntouched()
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
    public void Apply_WhenEnumHeaderParameter_ShouldLeaveSchemaUntouched()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Type = JsonSchemaType.String
        };
        var operation = OperationWith("X-Dummy", ParameterLocation.Header, schema);
        var context = BuildContext(new ApiParameterDescription { Name = "X-Dummy", Type = typeof(DummyEnum) });

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
    [InlineData("ValueTwo", HttpStatusCode.OK, "ValueTwo")]
    [InlineData("0", HttpStatusCode.OK, "ValueOne")]
    [InlineData("99", HttpStatusCode.OK, "99")]
    [InlineData("valueOne", HttpStatusCode.BadRequest, null)]
    [InlineData("banana", HttpStatusCode.BadRequest, null)]
    public async Task EnumQueryBinding_IsCaseSensitiveAndNumeric(
        string value,
        HttpStatusCode expectedStatus,
        string? expectedBody)
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        app.MapGet("/bind", (DummyEnum dummyEnum) => Results.Text(dummyEnum.ToString()));
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

    private static OpenApiOperation OperationWith(string name, ParameterLocation location, OpenApiSchema? schema = null)
        => new()
        {
            Parameters =
            [
                new OpenApiParameter
                {
                    Name = name,
                    In = location,
                    Schema = schema ?? new OpenApiSchema { Type = JsonSchemaType.String },
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

        var method = typeof(FixEnumCaseParameterFilterTests)
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

internal enum DummyEnum
{
    ValueOne,
    ValueTwo,
}
