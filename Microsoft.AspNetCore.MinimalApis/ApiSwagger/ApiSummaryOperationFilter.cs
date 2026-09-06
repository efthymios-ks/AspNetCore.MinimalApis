using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace Microsoft.AspNetCore.MinimalApis.ApiSwagger;

internal sealed class ApiSummaryOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var endpointType = context.MethodInfo.DeclaringType
            ?? throw new InvalidOperationException("Unable to determine endpoint type for summary");

        var summaryType = endpointType
            .Assembly
            .GetTypes()
            .FirstOrDefault(type
                => type.IsClass
                && !type.IsAbstract
                && type.IsAssignableTo(typeof(ApiSummary<>).MakeGenericType(endpointType))
            );

        if (summaryType is null)
        {
            return;
        }

        var summary = Activator.CreateInstance(summaryType);
        var applyMethod = summaryType
            .GetMethod(nameof(ApiSummary<>.Apply), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
        applyMethod.Invoke(summary, [operation]);
    }
}
