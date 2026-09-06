# FastEndpoints → Microsoft.AspNetCore.MinimalApis Reference

## Table of Contents

- [FastEndpoints → Microsoft.AspNetCore.MinimalApis Reference](#fastendpoints--commonminimalapis-reference)
  - [Table of Contents](#table-of-contents)
  - [Registration](#registration)
  - [Endpoint Definition](#endpoint-definition)
    - [Constructor injection → method injection](#constructor-injection--method-injection)
    - [Send methods → Results](#send-methods--results)
  - [Authorization](#authorization)
  - [Swagger Metadata](#swagger-metadata)
    - [Response types and tags](#response-types-and-tags)
    - [Summaries](#summaries)
  - [Validation](#validation)
  - [Versioning](#versioning)
  - [Testing](#testing)
    - [Comparison](#comparison)
  - [Feature Parity](#feature-parity)

---

## Registration

**FastEndpoints:**
```csharp
services.AddFastEndpoints()
    .SwaggerDocument(options =>
    {
        options.DocumentSettings = settings =>
        {
            settings.DocumentName = "Release 1";
            settings.Title = "My API";
            settings.Version = "v1";
        };
    });

app.UseFastEndpoints(config =>
{
    config.Endpoints.RoutePrefix = "api";
    config.Versioning.Prefix = "v";
    config.Versioning.PrependToRoute = true;
}).UseSwaggerGen();
```

**Microsoft.AspNetCore.MinimalApis:**
```csharp
services.AddApiEndpoints();
services.AddSwaggerGen(options => options.ConfigureApiEndpoints());

app.UseSwagger();
app.UseSwaggerUI();
await app.UseApiEndpointsAsync(options =>
{
    options.RoutePrefix = "api";
});
```

**Swagger `servers` — set `document.Servers` so "Try it out" targets the real host:** NSwag (what FastEndpoints
used) auto-populated the OpenAPI `servers` array from the incoming request, so Swagger UI's "Try it out" hit the
correct host. Swashbuckle emits **no** `servers` entry by default, so behind the API gateway/reverse proxy "Try it
out" resolves against the wrong base and returns an nginx 404. Add a `PreSerializeFilters` callback to set
`document.Servers`, forcing an `https` entry because the service sits behind a TLS-terminating proxy:

```csharp
app.UseSwagger(options =>
{
    options.PreSerializeFilters.Add((document, request) =>
    {
        document.Servers =
        [
            new () { Url = $"https://{request.Host.Value}{request.PathBase.Value}" }
        ];
    });
});
```

**JSON casing (do not skip):** minimal APIs serialize at runtime via `Http.Json.JsonOptions`
(`ConfigureHttpJsonOptions`), but Swashbuckle reads a **different** object, `Microsoft.AspNetCore.Mvc.JsonOptions`.
Pick the policy the contract requires **explicitly** and apply it to **both** (one shared helper) — the defaults
differ (minimal APIs camelCase, Swashbuckle PascalCase), so relying on them drifts the runtime output from the doc:

```csharp
static void ConfigureJson(JsonSerializerOptions o) {
    o.PropertyNamingPolicy = null;                       // the policy the contract requires (null = PascalCase)
    o.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
}
services.ConfigureHttpJsonOptions(o => ConfigureJson(o.SerializerOptions));                             // runtime
services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(o => ConfigureJson(o.JsonSerializerOptions));  // Swagger
```

**`ApiEnum<TEnum>` — case-insensitive enum binding for query/route params:** plain Minimal-API binding is
case-sensitive `Enum.TryParse`, so `"oneWay"` is rejected; NSwag/FastEndpoints bound enums case-insensitively.
`ApiEnum<TEnum>` (a `readonly record struct` implementing `IParsable`, namespace `AegeanApi.Microsoft.AspNetCore.MinimalApis`,
in the `AegeanApi.Microsoft.AspNetCore.MinimalApis` package) parses enum values **case-insensitively and by numeric value** for
Minimal-API route/query binding and implicitly converts to/from `TEnum`. **Sweep this service's own `Contracts.*`
projects** — the business repo's PUBLIC request/DTO contracts (e.g. `Contracts.Api`, `Contracts.Api.Bff`), *not*
the common library's contracts — for `[AsParameters]` query/route request-model properties whose type is a bare
`enum`, and convert each to `ApiEnum<…>`:

```csharp
using AegeanApi.Microsoft.AspNetCore.MinimalApis;
// before: public required TripType TripType;
public required ApiEnum<TripType> TripType;
```

No handler changes — the implicit conversions keep comparisons like `request.TripType == TripType.OneWay`
compiling. Swagger renders `ApiEnum<>` params as a plain string-enum dropdown via the internal
`ApiEnumParameterFilter` (`AegeanApi.Microsoft.AspNetCore.MinimalApis.ApiSwagger`), **auto-registered by
`ConfigureApiEndpoints()`** — no manual `OperationFilter` registration needed.

---

## Endpoint Definition

### Constructor injection → method injection

**FastEndpoints** uses constructor injection (primary constructor pattern):
```csharp
public class GetProductsEndpoint(IProductRepository repository)
    : Endpoint<EmptyRequest, List<Product>>
{
    public override void Configure()
    {
        Get("/products");
        AllowAnonymous();
    }

    public override async Task HandleAsync(EmptyRequest req, CancellationToken ct)
    {
        var products = await repository.GetAllAsync(ct);
        await SendOkAsync(products, ct);
    }
}
```

**Microsoft.AspNetCore.MinimalApis** uses method-level DI:
```csharp
public sealed class GetProductsEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("/products", HandleAsync);

    private async Task<IResult> HandleAsync(
        IProductRepository repository,
        CancellationToken cancellationToken = default)
    {
        var products = await repository.GetAllAsync(cancellationToken);
        return Results.Ok(products);
    }
}
```

### Send methods → Results

| FastEndpoints | Microsoft.AspNetCore.MinimalApis |
|---|---|
| `await SendOkAsync(value)` | `return Results.Ok(value)` |
| `await SendAsync(value, StatusCodes.Status201Created)` | `return Results.Created(location, value)` |
| `await SendNoContentAsync()` | `return Results.NoContent()` |
| `await SendNotFoundAsync()` | `return Results.NotFound()` |
| `await SendErrorsAsync()` | `return Results.ValidationProblem(errors)` |
| `await SendForbiddenAsync()` | `return Results.Forbid()` |
| `await SendUnauthorizedAsync()` | `return Results.Unauthorized()` |

---

## Authorization

**FastEndpoints:**
```csharp
public override void Configure()
{
    Post("/products");
    AllowAnonymous();       // no auth
    // or: Roles("Admin");  // require role
    // or: (nothing)        // requires auth by default if global policy set
}
```

**Microsoft.AspNetCore.MinimalApis:**
```csharp
public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
    => app.MapPost("/products", HandleAsync)
        .AllowAnonymous()           // no auth
        .RequireAuthorization()     // require auth
        .RequireAuthorization("AdminPolicy"); // require named policy
```

---

## Swagger Metadata

### Response types and tags

**FastEndpoints:**
```csharp
public override void Configure()
{
    Post("/products");
    Description(options => options
        .Produces<Product>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithTags("Products")
    );
}
```

**Microsoft.AspNetCore.MinimalApis:**
```csharp
public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
    => app.MapPost("/products", HandleAsync)
        .Produces<Product>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithTags(EndpointTags.Products);
```

### Summaries

**FastEndpoints:**
```csharp
public sealed class CreateProductEndpointSummary : Summary<CreateProductEndpoint>
{
    public CreateProductEndpointSummary()
    {
        Summary = "Create a new product";
        Description = Summary;
        RequestExamples.Add(new(new CreateProductRequest { Name = "Smartphone", Price = 499.99m }, "Simple request"));
    }
}
```

**Microsoft.AspNetCore.MinimalApis:**
```csharp
public sealed class CreateProductSummary : ApiSummary<CreateProductEndpoint>
{
    public CreateProductSummary()
    {
        Summary = "Creates a new product";
        Description = Summary;
        AddBodyExample("Simple request", new CreateProductRequest { Name = "Smartphone", Price = 499.99m });

        // Response examples (FastEndpoints: s.ResponseExamples[StatusCodes.Status200OK] = ...)
        AddResponseExample(StatusCodes.Status200OK, "Created", new Product { Id = 1, Name = "Smartphone", Price = 499.99m });
    }
}
```

| FastEndpoints | Microsoft.AspNetCore.MinimalApis |
|---|---|
| `RequestExamples.Add(new(req, "name"))` | `AddBodyExample("name", req)` |
| `s.ResponseExamples[StatusCodes.Status200OK] = resp` | `AddResponseExample(StatusCodes.Status200OK, "name", resp)` |
| `s.Params["x"] = "..."` | `AddParameterExample("x", value)` — or `AddParameterExamples(dto)` to map every property of a DTO by name (case-insensitive) |

---

## Validation

FastEndpoints uses FluentValidation. Microsoft.AspNetCore.MinimalApis uses its **own** rule builder with a
FluentValidation-style API (a curated subset, plus extras). Only the base class changes for common
rules; a few names and the message/condition syntax differ.

**FastEndpoints:**
```csharp
public sealed class CreateProductRequestValidator : Validator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(100);
        RuleFor(r => r.Price).GreaterThan(0);
    }
}
```

**Microsoft.AspNetCore.MinimalApis:**
```csharp
public sealed class CreateProductRequestValidator : ApiValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(100);
        RuleFor(r => r.Price).GreaterThan(0);
    }
}
```

Both are auto-discovered. No explicit registration needed.

### Rule mapping

| FluentValidation (FastEndpoints) | Microsoft.AspNetCore.MinimalApis `ApiValidator<T>` |
|---|---|
| `NotEmpty` / `NotNull` / `Null` / `Empty` | same — `NotEmpty`/`Empty` are FV-compatible: treat null, **whitespace strings**, empty collections, and **default value-types** (`Guid.Empty`, `0`) as empty |
| `Equal` / `NotEqual` | same |
| `GreaterThan(OrEqualTo)` / `LessThan(OrEqualTo)` / `InclusiveBetween` / `ExclusiveBetween` | same |
| `Length(exact)` / `Length(min,max)` | same |
| `MaximumLength` / `MinimumLength` | same |
| `Matches` / `EmailAddress` / `CreditCard` | same (plus `Contains`/`StartsWith`/`EndsWith`/`AbsoluteUrl`/`RelativeUrl`/`MustBeUrl`) |
| `IsInEnum` | same |
| `Must(predicate)` | `Must(predicate)` / `Must((root, value) => ...)` |
| `SetValidator(v)` | `SetValidator(v)` |
| `ChildRules(c => ...)` | `ChildRules(c => ...)` (single object) |
| `RuleForEach(x => x.Items)...` | `RuleFor(x => x.Items).ForEach<TElement>(e => e.RuleFor(...)...)` |
| `Include(otherValidator)` | `Include(otherValidator)` |

### Messages, names, conditions — chained, not arguments

In Microsoft.AspNetCore.MinimalApis these are **chained modifiers** on the preceding rule, not parameters:

```csharp
// FastEndpoints
RuleFor(r => r.Code).NotEmpty().WithMessage("Code is required").When(r => r.RequiresCode);

// Microsoft.AspNetCore.MinimalApis — identical shape
RuleFor(r => r.Code).NotEmpty().WithMessage("Code is required").When(r => r.RequiresCode);
```

- `WithMessage(string)` — overrides the failure message (there is no per-rule `message` argument).
- `WithName(string)` — overrides the reported member name.
- `When(predicate)` / `Unless(predicate)` — chained form guards **all** preceding rules in the chain.
  The block form `When(predicate, () => { RuleFor(...)...; })` / `Unless(...)` is **also** supported (gates
  every rule inside), mirroring FluentValidation. Both `RuleForEach`'s replacement (`ForEach<T>`) and these
  are available, so a FastEndpoints validator can usually be ported almost verbatim.
- `WithErrorCode(string)` — attaches an error code (stored on the result; the HTTP response stays message-only).

### Not supported (rewrite during migration)

- **`MustAsync` / async rules** — the pipeline is synchronous. Validators enforce data integrity;
  move async/business checks (DB or remote lookups) into the handler.

---

## Versioning

**FastEndpoints:**
```csharp
// Endpoint
public override void Configure()
{
    Post("/products");
    Version(1, deprecateAt: 2);
}

// Global config
app.UseFastEndpoints(config =>
{
    config.Versioning.Prefix = "v";
    config.Versioning.PrependToRoute = true;
});
```

**Microsoft.AspNetCore.MinimalApis:**
```csharp
// V1 endpoint — separate class
[ApiVersionGroup("create-product")]
[ApiVersion(1, Deprecated = true)]
public sealed class CreateProductEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("/products", HandleAsync); // → /api/v1/products
}

// V2 endpoint — separate class
[ApiVersionGroup("create-product")]
[ApiVersion(2)]
public sealed class CreateProductV2Endpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("/products", HandleAsync); // → /api/v2/products
}
```

**Key difference**: FastEndpoints uses a single class with `Version()` in `Configure()`. Microsoft.AspNetCore.MinimalApis requires a **separate class per version** with attributes.

---

## Testing

**FastEndpoints** (`Factory.Create`) — uses a real HTTP client:
```csharp
var client = Factory.Create<CreateProductEndpoint>(app =>
{
    app.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
});

var response = await client.PostAsJsonAsync("/api/products", new CreateProductRequest
{
    Name = "Smartphone",
    Price = 499.99m
});

Assert.Equal(HttpStatusCode.OK, response.StatusCode);
var product = await response.Content.ReadFromJsonAsync<Product>();
```

**Microsoft.AspNetCore.MinimalApis** (`ApiEndpointContext.Create`) — in-process, no HTTP stack:
```csharp
await using var context = ApiEndpointContext.Create<CreateProductEndpoint>(services =>
    services.AddSingleton<IProductRepository, InMemoryProductRepository>());

// Inspect metadata without invoking
Assert.Equal("/products", context.Metadata.Route);
Assert.Equal(HttpMethods.Post, context.Metadata.HttpMethod);
Assert.False(context.Metadata.RequiresAuthorization);

// Mutate context before invoking
context.HttpContext.Request.Headers["X-Correlation-Id"] = "abc-123";

// Invoke and assert
var result = await context.InvokeAsync();
var statusCodeResult = Assert.IsType<IStatusCodeHttpResult>(result, exactMatch: false);
Assert.Equal(StatusCodes.Status200OK, statusCodeResult.StatusCode);

// Read response body
context.HttpContext.Response.Body.Position = 0;
var body = await new StreamReader(context.HttpContext.Response.Body).ReadToEndAsync();
Assert.NotEmpty(body);
```

### Comparison

| Aspect | FastEndpoints `Factory.Create` | `ApiEndpointContext.Create` |
|---|---|---|
| HTTP stack | Full (TestServer) | None (in-process) |
| Returns | `HttpClient` | `ApiEndpointContext` (`IAsyncDisposable` — always `await using`) |
| Assert on | `HttpResponseMessage` | `object?` — cast to `IResult`, plain value, or `EmptyHttpResult` |
| Metadata access | No | Yes (`Route`, `HttpMethod`, `RequiresAuthorization`, `Metadata`, `Version`, `ClassAttributes`) |
| Speed | Slower (full pipeline) | Faster (handler only) |
| Filters run | All (auth, validation, etc.) | Validation filter not auto-added |

---

## Feature Parity

| Feature | FastEndpoints | Microsoft.AspNetCore.MinimalApis |
|---|---|---|
| Endpoint base class | `Endpoint<TReq, TRes>` | `ApiEndpoint` |
| Route/method config | `Configure()` | `MapEndpoint()` fluent |
| DI | Constructor | Handler method parameters |
| Validation | `Validator<T>` (FluentValidation) | `ApiValidator<T>` (custom rule builder; chained `WithMessage`/`When`/`Unless`; no async) |
| Versioning | `Version()` in Configure | `[ApiVersionGroup]` + `[ApiVersion]` attributes |
| Swagger summary | `Summary<T>` | `ApiSummary<T>` |
| Request / response examples | `RequestExamples` / `ResponseExamples` | `AddBodyExample` / `AddResponseExample` |
| Global filters | `config.Middleware` | `options.AddGlobalEndpointFilter<T>()` |
| Testing | `Factory.Create` (HttpClient) | `ApiEndpointContext.Create` (IResult) |
| Caching | Not built-in | `WithLocalApiCaching` / `WithDistributedApiCaching` |
| Idempotency | Not built-in | `WithApiIdempotency` |
| Request logging | Not built-in | `UseApiRequestLog` / `WithAdditionalLogProperties` |
