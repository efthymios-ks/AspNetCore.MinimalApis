namespace Microsoft.AspNetCore.MinimalApis.ApiVersions;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ApiVersionGroupAttribute : Attribute
{
    public ApiVersionGroupAttribute(string group)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(group);

        Group = group;
    }

    public string Group { get; }
}
