using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Nodes;

namespace Microsoft.AspNetCore.MinimalApis.ApiSwagger;

/// <summary>
/// Rewrites <see cref="ApiEnum{TEnum}"/> route and query parameters into a plain string-enum schema
/// (the wrapped enum's member names), so Swagger renders a dropdown instead of the wrapper's object
/// shape. The wrapper's auto-generated example/default (an object such as <c>{ "Value": "RoundTrip" }</c>)
/// is discarded in favour of a valid member name. Complements <see cref="FixEnumCaseParameterFilter"/>,
/// which handles bare enum parameters.
/// </summary>
internal sealed class ApiEnumParameterFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters is null)
        {
            return;
        }

        foreach (var openApiParameter in operation.Parameters)
        {
            if (openApiParameter is not OpenApiParameter parameter)
            {
                continue;
            }

            if (!TryGetWrappedEnumType(context, parameter.Name, out var enumType))
            {
                continue;
            }

            var enumValues = Enum.GetNames(enumType!);
            var defaultValue = TryGetEnumName(parameter.Example, enumValues)
                ?? TryGetEnumName(parameter.Schema?.Default, enumValues)
                ?? enumValues.FirstOrDefault();

            parameter.Content = null;
            parameter.Example = null;
            parameter.Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Enum = [.. enumValues.Select(name => (JsonNode)JsonValue.Create(name)!)],
                Default = JsonValue.Create(defaultValue),
            };
        }
    }

    private static bool TryGetWrappedEnumType(OperationFilterContext context, string? parameterName, out Type? enumType)
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

        if (parameterType is null
            || !parameterType.IsGenericType
            || parameterType.GetGenericTypeDefinition() != typeof(ApiEnum<>))
        {
            return false;
        }

        enumType = parameterType.GetGenericArguments()[0];
        return true;
    }

    private static string? TryGetEnumName(JsonNode? candidate, string[] enumNames)
    {
        var text = candidate switch
        {
            JsonValue value when value.TryGetValue<string>(out var stringValue) => stringValue,
            JsonObject wrapper => GetWrappedString(wrapper),
            _ => null,
        };

        return text is null
            ? null
            : enumNames.FirstOrDefault(name => string.Equals(name, text, StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetWrappedString(JsonObject wrapper)
    {
        foreach (var property in wrapper)
        {
            if (property.Value is JsonValue value && value.TryGetValue<string>(out var text))
            {
                return text;
            }
        }

        return null;
    }
}
