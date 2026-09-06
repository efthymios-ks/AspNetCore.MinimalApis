namespace Microsoft.AspNetCore.MinimalApis.ApiSwagger;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public abstract class ApiParameterDropdownAttributeBase
    : ApiParameterBase
{
    public abstract IEnumerable<string> Values { get; }
}
