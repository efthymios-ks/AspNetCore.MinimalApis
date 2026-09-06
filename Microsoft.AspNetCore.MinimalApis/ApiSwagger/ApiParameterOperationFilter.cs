using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace Microsoft.AspNetCore.MinimalApis.ApiSwagger;

internal sealed class ApiParameterOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var declaringType = context.MethodInfo.DeclaringType;
        if (declaringType is null)
        {
            return;
        }

        operation.Parameters ??= [];

        foreach (var parameter in GetParameters(declaringType))
        {
            operation.Parameters.Add(parameter);
        }

        foreach (var dropdownParameter in GetDropdownParameters(declaringType))
        {
            operation.Parameters.Add(dropdownParameter);
        }
    }

    private static IEnumerable<IOpenApiParameter> GetParameters(Type declaringType)
    {
        var parameters = declaringType
            .GetCustomAttributes(typeof(ApiParameterBase), true)
            .OfType<ApiParameterBase>()
            .Where(attribute => attribute is ApiHeaderAttribute or ApiQueryAttribute)
            .ToArray();

        foreach (var parameter in parameters)
        {
            var openApiParameter = new OpenApiParameter
            {
                Name = parameter.Key,
                In = parameter.Location,
                Required = parameter.IsRequired,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Default = parameter.DefaultValue is not null
                        ? JsonValue.Create(parameter.DefaultValue)
                        : null
                }
            };

            yield return openApiParameter;
        }
    }

    private static IEnumerable<IOpenApiParameter> GetDropdownParameters(Type declaringType)
    {
        var dropdownParameters = declaringType
            .GetCustomAttributes(typeof(ApiParameterDropdownAttributeBase), true)
            .OfType<ApiParameterDropdownAttributeBase>()
            .ToArray();

        foreach (var parameter in dropdownParameters)
        {
            var values = parameter
                .Values
                .Select(value => value.Trim())
                .ToArray();

            var defaultValue = parameter.DefaultValue?.Trim();
            if (!string.IsNullOrWhiteSpace(defaultValue) && !values.Contains(defaultValue))
            {
                throw new InvalidOperationException(
                    $"The default value '{defaultValue}' is not in the list of valid values for the {parameter.Location} '{parameter.Key}'."
                );
            }

            var openApiParameter = new OpenApiParameter
            {
                Name = parameter.Key,
                In = parameter.Location,
                Required = parameter.IsRequired,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Enum = [.. values.Select(value => JsonValue.Create(value))],
                    Default = parameter.DefaultValue is not null
                        ? JsonValue.Create(parameter.DefaultValue)
                        : null
                }
            };

            yield return openApiParameter;
        }
    }
}
