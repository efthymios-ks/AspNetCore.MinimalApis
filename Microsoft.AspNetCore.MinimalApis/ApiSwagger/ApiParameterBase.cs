using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi;
using System.Collections.Concurrent;

namespace Microsoft.AspNetCore.MinimalApis.ApiSwagger;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public abstract class ApiParameterBase : Attribute
{
    private static readonly ConcurrentDictionary<Type, Lazy<Task>> _configurations = new();

    public abstract ParameterLocation Location { get; }
    public abstract string Key { get; }
    public virtual string? DefaultValue { get; }
    public virtual bool IsRequired { get; }

    public virtual Task ConfigureAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IWebHostEnvironment environment
    ) => Task.CompletedTask;

    internal Task ConfigureInternalAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IWebHostEnvironment environment
    ) => _configurations.GetOrAdd(
        GetType(),
        _ => new Lazy<Task>(() => ConfigureAsync(services, configuration, environment))
    ).Value;

    internal static void ClearConfigurations()
        => _configurations.Clear();
}
