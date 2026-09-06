using Asp.Versioning;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.AspNetCore.MinimalApis.ApiSwagger;

internal sealed class ApiVersionDeprecatedOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var declaringType = context.MethodInfo.DeclaringType!;
        var isDeprecated = declaringType
            .GetCustomAttributes(typeof(ApiVersionAttribute), true)
            .OfType<ApiVersionAttribute>()
            .Any(attr => attr.Deprecated);

        if (isDeprecated)
        {
            operation.Deprecated = true;
        }
    }
}
