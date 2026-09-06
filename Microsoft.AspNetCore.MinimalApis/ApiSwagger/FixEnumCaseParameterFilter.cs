using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Nodes;

namespace Microsoft.AspNetCore.MinimalApis.ApiSwagger;

/// <summary>
/// Rewrites enum path and query parameters so Swagger offers their exact .NET member names — the PascalCase values <c>Enum.ToString()</c> emits. 
/// Minimal-API route/query binding parses enums with a case-sensitive <c>Enum.TryParse</c>, 
/// so the camelCase values the JSON naming policy would otherwise render (e.g. <c>"oneWay"</c>) are rejected by the binder. 
/// Any existing default or example on the parameter is preserved.
/// </summary>
internal sealed class FixEnumCaseParameterFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters is null)
        {
            return;
        }

        foreach (var openApiParameter in operation.Parameters)
        {
            if (!TryGetPathParameter(openApiParameter, out var parameter))
            {
                continue;
            }

            if (!TryGetEnumType(context, parameter!.Name, out var enumType))
            {
                continue;
            }

            var enumValues = Enum.GetNames(enumType!);
            var defaultValue = parameter.Schema?.Default
                ?? parameter.Example
                ?? enumValues.FirstOrDefault();

            parameter.Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Enum = [.. enumValues.Select(name => (JsonNode)JsonValue.Create(name)!)],
                Default = defaultValue,
            };
        }
    }

    private static bool TryGetPathParameter(IOpenApiParameter parameter, out OpenApiParameter? pathParameter)
    {
        pathParameter = null;
        if (parameter is not OpenApiParameter castParameter)
        {
            return false;
        }

        if (parameter.In is not ParameterLocation.Path and not ParameterLocation.Query)
        {
            return false;
        }

        pathParameter = castParameter;
        return true;
    }

    private static bool TryGetEnumType(OperationFilterContext context, string? parameterName, out Type? enumType)
    {
        enumType = null;
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            return false;
        }

        var parameterType = context
            .ApiDescription
            .ParameterDescriptions
            .FirstOrDefault(description
                => string.Equals(description.Name, parameterName, StringComparison.OrdinalIgnoreCase))?
            .Type;

        if (parameterType is null)
        {
            return false;
        }

        parameterType = Nullable.GetUnderlyingType(parameterType) ?? parameterType;
        if (!parameterType.IsEnum)
        {
            return false;
        }

        enumType = parameterType;
        return true;
    }
}
