using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace Microsoft.AspNetCore.MinimalApis.ApiSwagger;

public static class Extensions
{
    public static SwaggerGenOptions ConfigureApiEndpoints(this SwaggerGenOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.OperationFilter<ApiParameterOperationFilter>();
        options.OperationFilter<ApiVersionDeprecatedOperationFilter>();
        options.OperationFilter<ApiSummaryOperationFilter>();
        options.OperationFilter<FixEnumCaseParameterFilter>();
        options.OperationFilter<ApiEnumParameterFilter>();

        return options;
    }

    internal static Task ConfigureApiSwaggerAsync(this WebApplication app)
        => app.ConfigureApiSwaggerAsync(Assembly.GetEntryAssembly()!, Assembly.GetExecutingAssembly()!);

    internal static async Task ConfigureApiSwaggerAsync(this WebApplication app, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(app);

        var apiParameterAttributes = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type
                => type.IsClass
                && !type.IsAbstract
                && type.IsAssignableTo(typeof(ApiEndpoint))
            )
            .SelectMany(type => type.GetCustomAttributes<ApiParameterBase>(true))
            .ToArray();

        foreach (var apiParameterAttribute in apiParameterAttributes)
        {
            await apiParameterAttribute.ConfigureInternalAsync(app.Services, app.Configuration, app.Environment);
        }

        var apiSummaryTypes = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type
                => type.IsClass
                && !type.IsAbstract
                && IsApiSummary(type)
            )
            .ToArray();

        foreach (var apiSummaryType in apiSummaryTypes)
        {
            var apiSummary = Activator.CreateInstance(apiSummaryType)!;
            var configureTask = (Task)apiSummaryType
                .GetMethod(
                    nameof(ApiSummary<>.ConfigureAsync),
                    BindingFlags.Public | BindingFlags.Instance
                )!
                .Invoke(apiSummary, [app.Services, app.Configuration, app.Environment])!;

            await configureTask;
        }
    }

    private static bool IsApiSummary(Type type)
    {
        for (var currentType = type.BaseType;
            currentType is not null;
            currentType = currentType.BaseType
        )
        {
            if (currentType.IsGenericType
                && currentType.GetGenericTypeDefinition() == typeof(ApiSummary<>)
            )
            {
                return true;
            }
        }

        return false;
    }
}
