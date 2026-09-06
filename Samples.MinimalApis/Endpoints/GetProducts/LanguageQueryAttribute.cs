using Microsoft.AspNetCore.MinimalApis.ApiSwagger;

namespace Samples.MinimalApis.Endpoints.GetProducts;

public sealed class LanguageQueryAttribute
    : ApiQueryDropdownAttributeBase
{
    private static string _key = null!;
    private static string[] _values = null!;
    private static string _defaultValue = null!;

    public override string Key => _key;
    public override IEnumerable<string> Values => _values;
    public override string? DefaultValue => _defaultValue;
    public override bool IsRequired => false;

    public override Task ConfigureAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IWebHostEnvironment environment
    )
    {
        var section = configuration.GetSection("Localization");

        _key = section.GetValue<string>("QueryKey")
            ?? throw new InvalidOperationException("QueryKey is not configured.");

        _values = section.GetSection("SupportedCultures").Get<string[]>()
            ?? throw new InvalidOperationException("Values are not configured.");

        _defaultValue = section.GetValue<string>("DefaultCulture")
            ?? throw new InvalidOperationException("DefaultValue is not configured.");

        return Task.CompletedTask;
    }
}
