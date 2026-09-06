namespace Microsoft.AspNetCore.MinimalApis.ApiSwagger;

public static class SchemaIdExtensions
{
    /// <summary>
    /// Produces a unique, OpenAPI-valid schema id for a type, suitable for
    /// <c>SwaggerGenOptions.CustomSchemaIds(type =&gt; type.GetSafeSchemaId())</c>.
    /// <para>
    /// Swashbuckle's default selector uses the short type name, which collides when types share a name
    /// across namespaces (e.g. <c>Card</c>, <c>Sender</c>). This uses the full name to disambiguate,
    /// and composes generic arguments readably (e.g. <c>ResponseEnvelopeOf...Product</c>) instead of the
    /// assembly-qualified form that <see cref="Type.FullName"/> yields for closed generics.
    /// </para>
    /// </summary>
    public static string GetSafeSchemaId(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!type.IsGenericType)
        {
            return type.FullName?.Replace('+', '.') ?? type.Name;
        }

        var name = type.Name[..type.Name.IndexOf('`')];
        var arguments = string.Join("And", type.GetGenericArguments().Select(GetSafeSchemaId));
        return $"{type.Namespace}.{name}Of{arguments}";
    }
}
