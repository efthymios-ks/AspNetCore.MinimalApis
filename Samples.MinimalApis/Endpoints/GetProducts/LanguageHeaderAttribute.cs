using Microsoft.AspNetCore.MinimalApis.ApiSwagger;

namespace Samples.MinimalApis.Endpoints.GetProducts;

public sealed class LanguageHeaderAttribute
    : ApiHeaderDropdownAttributeBase
{
    public override string Key { get; } = "X-Language";
    public override IEnumerable<string> Values { get; } = ["en", "el", "de"];
    public override string? DefaultValue { get; } = "en";
    public override bool IsRequired { get; } = false;
}
