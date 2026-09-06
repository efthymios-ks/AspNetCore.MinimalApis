using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Text.Json;

namespace Microsoft.AspNetCore.MinimalApis.ApiSwagger;

public abstract class ApiSummary<TEndpoint>
    where TEndpoint : ApiEndpoint
{
    public string? Summary { get; set; }
    public string? Description { get; set; }

    private readonly Dictionary<string, object> _bodyExamples = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, object> _parameterExamples = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, Dictionary<string, object>> _responseExamples = [];

    public void AddParameterExample(string paramName, object value)
        => _parameterExamples[paramName] = value;

    /// <summary>
    /// Registers a parameter example per readable property of <paramref name="parameters"/>,
    /// keyed by property name. Null property values are skipped. Parameter-name matching is
    /// case-insensitive, so PascalCase DTO properties match camelCased OpenAPI parameters.
    /// </summary>
    public void AddParameterExamples<TParameters>(TParameters parameters)
        where TParameters : class
    {
        ArgumentNullException.ThrowIfNull(parameters);

        foreach (var property in typeof(TParameters).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var value = property.GetValue(parameters);
            if (value is not null)
            {
                _parameterExamples[property.Name] = value;
            }
        }
    }

    public void AddBodyExample(string name, object value)
        => _bodyExamples[name] = value;

    public void AddResponseExample(HttpStatusCode statusCode, string name, object value)
        => AddResponseExample((int)statusCode, name, value);

    public void AddResponseExample(int statusCode, string name, object value)
    {
        if (!_responseExamples.TryGetValue(statusCode, out var examples))
        {
            examples = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            _responseExamples[statusCode] = examples;
        }

        examples[name] = value;
    }

    public virtual Task ConfigureAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IWebHostEnvironment environment
    ) => Task.CompletedTask;

    internal void Apply(OpenApiOperation operation)
    {
        operation.Summary = Summary;
        operation.Description = Description;
        ApplyParameterExamples(operation);
        ApplyBodyExamples(operation);
        ApplyResponseExamples(operation);
    }

    private void ApplyResponseExamples(OpenApiOperation operation)
    {
        if (_responseExamples.Count is 0 || operation.Responses is null)
        {
            return;
        }

        foreach (var (statusCode, examples) in _responseExamples)
        {
            if (operation.Responses.TryGetValue(statusCode.ToString(), out var response) is not true)
            {
                continue;
            }

            if (response is not OpenApiResponse concreteResponse)
            {
                continue;
            }

            concreteResponse.Content ??= new Dictionary<string, OpenApiMediaType>(StringComparer.OrdinalIgnoreCase);

            if (!concreteResponse.Content.TryGetValue(MediaTypeNames.Application.Json, out var mediaType))
            {
                mediaType = new OpenApiMediaType();
                concreteResponse.Content[MediaTypeNames.Application.Json] = mediaType;
            }

            mediaType.Examples ??= new Dictionary<string, IOpenApiExample>(StringComparer.OrdinalIgnoreCase);

            foreach (var (exampleName, exampleValue) in examples)
            {
                mediaType.Examples[exampleName] = new OpenApiExample
                {
                    Summary = exampleName,
                    Value = JsonSerializer.SerializeToNode(exampleValue)
                };
            }
        }
    }

    private void ApplyBodyExamples(OpenApiOperation operation)
    {
        if (_bodyExamples.Count is 0)
        {
            return;
        }

        if (operation.RequestBody?.Content?.TryGetValue(MediaTypeNames.Application.Json, out var mediaType) is not true)
        {
            return;
        }

        mediaType!.Examples ??= new Dictionary<string, IOpenApiExample>(StringComparer.OrdinalIgnoreCase);

        foreach (var (exampleName, exampleValue) in _bodyExamples)
        {
            mediaType.Examples[exampleName] = new OpenApiExample
            {
                Summary = exampleName,
                Value = JsonSerializer.SerializeToNode(exampleValue)
            };
        }
    }

    private void ApplyParameterExamples(OpenApiOperation operation)
    {
        if (_parameterExamples.Count is 0)
        {
            return;
        }

        if (operation.Parameters is null or { Count: 0 })
        {
            return;
        }

        foreach (var parameter in operation.Parameters.OfType<OpenApiParameter>())
        {
            if (parameter.Name is null)
            {
                continue;
            }

            if (_parameterExamples.TryGetValue(parameter.Name, out var value))
            {
                parameter.Example = JsonSerializer.SerializeToNode(value);
            }
        }
    }
}
