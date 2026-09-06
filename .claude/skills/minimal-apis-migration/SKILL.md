---
name: minimal-apis-migration
description: Migration guide from FastEndpoints to Microsoft.AspNetCore.MinimalApis in .NET Core. Use when migrating from FastEndpoints, converting Endpoint<TRequest TResponse> class to ApiEndpoint, replacing Configure() method with MapEndpoint, swapping Validator<T> for ApiValidator<T>, replacing Factory.Create with ApiEndpointContext.Create, converting Summary<T> to ApiSummary, migrating versioning from Version() method to ApiVersionGroup and ApiVersion attributes, comparing FastEndpoints vs Minimal API registration, AllowAnonymous vs RequireAuthorization, SendOkAsync vs Results.Ok, HandleAsync signature differences, or understanding equivalent patterns between the two libraries.
---

# FastEndpoints → Microsoft.AspNetCore.MinimalApis Migration

## When to Use

- Converting an existing FastEndpoints endpoint to `ApiEndpoint`
- Looking up the equivalent pattern for a FastEndpoints feature
- Verifying registration differences

---

## Registration

| FastEndpoints | Microsoft.AspNetCore.MinimalApis |
|---|---|
| `services.AddFastEndpoints()` | `services.AddApiEndpoints()` |
| `.SwaggerDocument(...)` | `services.AddSwaggerGen(options => options.ConfigureApiEndpoints())` |
| `app.UseFastEndpoints(config => config.Endpoints.RoutePrefix = "api")` | `await app.UseApiEndpointsAsync(options => options.RoutePrefix = "api")` |
| `app.UseSwaggerGen()` | `app.UseSwagger(); app.UseSwaggerUI()` |

**JSON casing (do not skip):** set the naming policy **explicitly** on **both** `ConfigureHttpJsonOptions`
(runtime) and `Microsoft.AspNetCore.Mvc.JsonOptions` (Swagger schema) — the defaults differ (minimal APIs
camelCase, Swashbuckle PascalCase), so relying on them drifts the runtime output from the doc. See REFERENCE.

---

## Endpoint Definition

**FastEndpoints:**
```csharp
public class CreateProductEndpoint(IProductRepository repository)
    : Endpoint<CreateProductRequest, Product>
{
    public override void Configure()
    {
        Post("/products");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateProductRequest req, CancellationToken ct)
    {
        var product = await repository.CreateAsync(req, ct);
        await SendOkAsync(product, ct);
    }
}
```

**Microsoft.AspNetCore.MinimalApis:**
```csharp
public sealed class CreateProductApiEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("/products", HandleAsync);

    private async Task<IResult> HandleAsync(
        CreateProductRequest request,
        IProductRepository repository,
        CancellationToken cancellationToken = default)
    {
        var product = await repository.CreateAsync(request, cancellationToken);
        return Results.Ok(product);
    }
}
```

**Key differences:**
- Dependencies move from the **constructor** to **handler method parameters**
- `SendOkAsync(value)` → `return Results.Ok(value)`
- `AllowAnonymous()` in `Configure()` → `.AllowAnonymous()` fluent on `MapPost`
- No generic type parameters on the class

---

## Validation

| FastEndpoints | Microsoft.AspNetCore.MinimalApis |
|---|---|
| `Validator<T>` | `ApiValidator<T>` |
| `RuleFor(...).MaximumLength(100)` | `RuleFor(...).MaximumLength(100)` (custom rule builder, FluentValidation-style) |
| `.WithMessage(...)` / `.When(...)` chained | same, chained (no per-rule `message` argument) |
| `SetValidator` / `ChildRules` / `RuleForEach` | `SetValidator` / `ChildRules` / `RuleFor(..).ForEach<T>(..)` |
| `MustAsync` (async) | not supported — move async checks to the handler |
| Auto-discovered by scanning | Auto-discovered by `AddApiEndpoints()` |

```csharp
// FastEndpoints
public sealed class CreateProductRequestValidator : Validator<CreateProductRequest>

// Microsoft.AspNetCore.MinimalApis
public sealed class CreateProductRequestValidator : ApiValidator<CreateProductRequest>
```

> `ApiValidator<T>` is a **custom** rule builder (not FluentValidation): a curated subset of rules
> plus extras, with messages/conditions chained. See [REFERENCE.md](REFERENCE.md#validation) for the
> full rule mapping.

---

## Versioning

| FastEndpoints | Microsoft.AspNetCore.MinimalApis |
|---|---|
| `Version(1, deprecateAt: 2)` in `Configure()` | `[ApiVersion(1, Deprecated = true)]` attribute |
| `config.Versioning.Prefix = "v"` global config | Prefix built-in (`/v1`, `/v2`) |
| Route group set automatically | `[ApiVersionGroup("group-name")]` attribute required |

**For complete comparison**: [REFERENCE.md](REFERENCE.md)

---

## Testing

| FastEndpoints | Microsoft.AspNetCore.MinimalApis |
|---|---|
| `Factory.Create<TEndpoint>(app => ...)` | `ApiEndpointContext.Create<TEndpoint>(services => ...)` |
| Returns `HttpClient` | Returns `ApiEndpointContext` |
| Full HTTP round-trip | In-process, no HTTP stack |

```csharp
// FastEndpoints
var client = Factory.Create<CreateProductEndpoint>(app => { ... });
var response = await client.PostAsJsonAsync("/products", request);

// Microsoft.AspNetCore.MinimalApis
await using var context = ApiEndpointContext.Create<CreateProductApiEndpoint>(services =>
    services.AddSingleton<IProductRepository, InMemoryProductRepository>());

var result = await context.InvokeAsync();
```

**For full testing comparison**: [REFERENCE.md](REFERENCE.md#testing)
