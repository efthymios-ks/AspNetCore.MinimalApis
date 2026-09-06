# Microsoft.AspNetCore.MinimalApis Reference

## Table of Contents

- [Registration Options](#registration-options)
- [Endpoint Patterns](#endpoint-patterns)
- [Swagger Parameters](#swagger-parameters)
- [Versioning](#versioning)
- [Validation](#validation)
- [Testing with ApiEndpointContext](#testing-with-apiendpointcontext)
- [Caching](#caching)
- [Idempotency](#idempotency)
- [Logging](#logging)
- [Analyzers](#analyzers)

---

## Registration Options

```csharp
// Scan entry assembly (default)
services.AddApiEndpoints();

// Scan specific assemblies
services.AddApiEndpoints(typeof(CreateProductEndpoint).Assembly);

// Global filters applied to every endpoint
await app.UseApiEndpointsAsync(options =>
{
    options.RoutePrefix = "api";
    options.AddGlobalEndpointFilter<MyFilter>();
    options.AddGlobalEndpointFilter((context, next) =>
    {
        // inline factory
        return next(context);
    });
});
```

### JSON serialization — configure runtime AND Swagger

Minimal APIs serialize at runtime through `Microsoft.AspNetCore.Http.Json.JsonOptions` (`ConfigureHttpJsonOptions`).
Swashbuckle builds its schema — property names, examples, enum values, query params — from a **different** options
object, `Microsoft.AspNetCore.Mvc.JsonOptions`, which a minimal-API app never configures on its own. Configure
**both** from one helper so the generated docs match what the API actually emits, and **pick the naming policy
explicitly** — leaving it to the defaults (minimal APIs → camelCase, Swashbuckle → PascalCase) lets the runtime
output and the Swagger doc drift silently:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

static void ConfigureJsonOptions(JsonSerializerOptions options)
{
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;   // set the policy your contract requires
}

services.ConfigureHttpJsonOptions(o => ConfigureJsonOptions(o.SerializerOptions));                            // runtime
services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(o => ConfigureJsonOptions(o.JsonSerializerOptions)); // Swagger schema
```

- `Microsoft.AspNetCore.Mvc.JsonOptions` needs **no** `AddControllers()` — the type is in the shared framework.
- Cached and replayed responses (distributed cache, idempotency) reuse the host's Http.Json options, so a cache
  hit serializes identically to a live response.
- `ConfigureApiEndpoints()` also normalizes **enum path/query parameters** to their exact PascalCase member names
  (what `Enum.ToString()` emits), so Swagger's dropdown values bind against the case-sensitive query binder.

---

## Endpoint Patterns

### Minimal endpoint

```csharp
public sealed class GetProductsEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("/products", HandleAsync);

    private async Task<IResult> HandleAsync(
        IProductRepository repository,
        CancellationToken cancellationToken = default
    )
    {
        var products = await repository.GetAllAsync(cancellationToken);
        return Results.Ok(products);
    }
}
```

### With full metadata

```csharp
public sealed class CreateProductEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("/products", HandleAsync)
            .Accepts<CreateProductRequest>(MediaTypeNames.Application.Json)
            .Produces<Product>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .WithTags(EndpointTags.Products);

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

### With authorization

```csharp
public sealed class DeleteProductEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
        => app.MapDelete("/products/{id}", HandleAsync)
            .RequireAuthorization();
}
```

---

## Swagger Parameters

### Static header or query

```csharp
[ApiHeader("X-Api-Key")]
[ApiHeader("X-Correlation-Id", "00000000-0000-0000-0000-000000000000")]
[ApiQuery("search")]
[ApiQuery("status", "active")]
public sealed class GetProductsEndpoint : ApiEndpoint { ... }
```

### Dropdown header

```csharp
public sealed class LanguageHeaderAttribute : ApiHeaderDropdownAttributeBase
{
    public override string Key { get; } = "X-Language";
    public override IEnumerable<string> Values { get; } = ["en", "el", "de"];
    public override string? DefaultValue { get; } = "en";
    public override bool IsRequired { get; } = false;
}

[LanguageHeader]
public sealed class GetProductsEndpoint : ApiEndpoint { ... }
```

### Dropdown query

```csharp
public sealed class SortOrderQueryAttribute : ApiQueryDropdownAttributeBase
{
    public override string Key { get; } = "sortOrder";
    public override IEnumerable<string> Values { get; } = ["asc", "desc"];
    public override string? DefaultValue { get; } = "asc";
    public override bool IsRequired { get; } = false;
}
```

### Dynamic dropdown (values from config at runtime)

```csharp
public sealed class RegionQueryAttribute : ApiQueryDropdownAttributeBase
{
    public override string Key => _key;
    public override IEnumerable<string> Values => _values;
    public override string? DefaultValue => _defaultValue;
    public override bool IsRequired => false;

    // Runs at most once per attribute type — the framework guards it, so no manual flag.
    public override Task ConfigureAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IWebHostEnvironment environment
    )
    {
        _key = configuration["Regions:QueryKey"]!;
        _values = configuration.GetSection("Regions:Available").Get<string[]>()!;
        _defaultValue = configuration["Regions:Default"]!;
        return Task.CompletedTask;
    }

    private static string _key = null!;
    private static string[] _values = null!;
    private static string _defaultValue = null!;
}
```

---

## Versioning

```csharp
// Endpoint groups share a version set — same Group string = same set
[ApiVersionGroup("create-product")]
[ApiVersion(1, Deprecated = true)]
public sealed class CreateProductEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("/products", HandleAsync); // → /api/v1/products
}

[ApiVersionGroup("create-product")]
[ApiVersion(2)]
public sealed class CreateProductV2Endpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("/products", HandleAsync); // → /api/v2/products
}
```

**Rules:**
- Both `[ApiVersionGroup]` and `[ApiVersion]` are required — one without the other throws
- Only major versions are supported — minor versions (e.g. `1.1`) throw
- Duplicate version in the same group throws

---

## Validation

```csharp
public sealed class CreateProductRequestValidator : ApiValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(100);
        RuleFor(request => request.Price).GreaterThan(0);
    }
}
```

`ApiValidatorFilter` is automatically added to every endpoint. Validation failures return `400 ValidationProblem`. No manual wiring needed.

---

## Testing with ApiEndpointContext

### Basic usage

```csharp
await using var context = ApiEndpointContext.Create<MyEndpoint>(services =>
{
    services.AddSingleton<IMyService, MyService>();
});
```

### ApiEndpointContext members

| Member | Type | Notes |
|---|---|---|
| `HttpContext` | `DefaultHttpContext` | Mutate before invoking (headers, path, body) |
| `Metadata` | `ApiMetadata` | Extracted at build time |
| `InvokeAsync()` | `Task<object?>` | Returns raw handler result: `IResult`, plain value, or `EmptyHttpResult` for void. Resets response body each call. |
| `WithRouteValue(key, value)` | `ApiEndpointContext` | Sets a route value. Fluent — returns `this`. |
| `WithRouteValues(values)` | `ApiEndpointContext` | Sets multiple route values. Fluent — returns `this`. |
| `WithQueryParam(key, value)` | `ApiEndpointContext` | Sets a query parameter. Fluent — returns `this`. |
| `WithQueryParams(values)` | `ApiEndpointContext` | Sets multiple query parameters. Fluent — returns `this`. |
| `WithHeader(key, value)` | `ApiEndpointContext` | Sets a request header. Fluent — returns `this`. |
| `WithHeaders(values)` | `ApiEndpointContext` | Sets multiple request headers. Fluent — returns `this`. |
| `WithJsonBody<TElement>(body, options?)` | `ApiEndpointContext` | Serializes `body` as JSON; sets `Body`, `BodyReader`, `Content-Type: application/json`, and `Content-Length`. Fluent — returns `this`. |
| `WithXmlBody<TElement>(body, settings?)` | `ApiEndpointContext` | Serializes `body` as XML; sets `Body`, `Content-Type: application/xml`, and `Content-Length`. Fluent — returns `this`. |
| `WithFormField(key, value)` | `ApiEndpointContext` | Sets a form field; auto-sets `Content-Type` if not already form type. Fluent — returns `this`. |
| `WithFormFields(values)` | `ApiEndpointContext` | Sets multiple form fields. Fluent — returns `this`. |
| `WithFormFile(fieldName, content, fileName, contentType)` | `ApiEndpointContext` | Adds a file to the multipart form; sets `Content-Type: multipart/form-data`. Fluent — returns `this`. |
| `WithCancellationToken(cancellationToken)` | `ApiEndpointContext` | Sets the request cancellation token. Fluent — returns `this`. |
| `ReadJsonBodyAsync(cancellationToken)` | `Task<string>` | Reads the response body as a raw JSON string. Resets stream position before reading. |
| `ReadJsonBodyAsync<TElement>(options?, cancellationToken)` | `Task<TElement?>` | Deserializes the response body as `TElement`. Resets stream position before reading. |
| `DisposeAsync()` | `ValueTask` | Disposes internal resources (response body, DI container). Always use `await using`. |

### ApiMetadata members

| Property | Type | Notes |
|---|---|---|
| `Route` | `string` | e.g. `"/products"` |
| `HttpMethod` | `string` | e.g. `"POST"` |
| `RequiresAuthorization` | `bool` | `true` when `IAuthorizeData` present and no `IAllowAnonymous` |
| `Metadata` | `IReadOnlyList<object>` | Full endpoint metadata collection |
| `Version` | `ApiVersionInfo?` | `null` unless both `[ApiVersionGroup]` and `[ApiVersion]` are present |
| `ClassAttributes` | `IReadOnlyList<Attribute>` | Attributes declared directly on the endpoint class |

### Full test example

```csharp
[Fact]
public async Task CreateProduct_WhenRequestIsValid_ShouldReturnOk()
{
    // Arrange
    await using var context = ApiEndpointContext.Create<CreateProductEndpoint>(services =>
        services.AddSingleton<IProductRepository, InMemoryProductRepository>());

    context.HttpContext.Request.Headers["X-Correlation-Id"] = "abc-123";

    // Act
    var result = await context.InvokeAsync();

    // Assert
    Assert.Equal("/products", context.Metadata.Route);
    Assert.Equal(HttpMethods.Post, context.Metadata.HttpMethod);

    var statusCodeResult = Assert.IsType<IStatusCodeHttpResult>(result, exactMatch: false);
    Assert.Equal(StatusCodes.Status200OK, statusCodeResult.StatusCode);

    var body = await context.ReadJsonBodyAsync();
    Assert.NotEmpty(body);
}
```

---

## Caching

### Local (in-memory)

```csharp
// Program.cs
services.AddLocalApiCaching(options =>
{
    options.CacheDuration = TimeSpan.FromMinutes(30);
    options.KeySuffixFactory = filterContext => filterContext.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
});

// Endpoint
app.MapGet("/products", HandleAsync)
    .WithLocalApiCaching(options => options.CacheDuration = TimeSpan.FromHours(1));
```

### Distributed (Redis or memory)

```csharp
// Program.cs
services.AddStackExchangeRedisCache(options => options.Configuration = "localhost:6379");
// or: services.AddDistributedMemoryCache();
services.AddDistributedApiCaching(options => options.CacheDuration = TimeSpan.FromMinutes(30));

// Endpoint
app.MapGet("/products", HandleAsync)
    .WithDistributedApiCaching(options => options.CacheDuration = TimeSpan.FromHours(1));
```

---

## Idempotency

`POST` only. First request runs the handler and stores its **2xx** response; duplicates
**replay** that stored response (same status + body). Duplicates while the first is still
in flight get `409`. A non-2xx or thrown response releases the reservation (client may
retry). A **non-empty key is required** — an empty `KeySuffixFactory` result yields `400`.

```csharp
// Program.cs
// Default store needs a registered IDistributedCache
// (AddStackExchangeRedisCache, or AddDistributedMemoryCache for tests/single-node).
services.AddApiIdempotency(options =>
{
    options.KeySuffixFactory = filterContext => filterContext.HttpContext.Request.Headers["Idempotency-Key"].ToString();
    options.CacheDuration = TimeSpan.FromMinutes(5);     // how long a completed response replays
    options.ProcessingTimeout = TimeSpan.FromSeconds(30); // in-flight reservation lifetime
});

// Endpoint
app.MapPost("/products", HandleAsync)
    .WithApiIdempotency();
```

The default `DistributedCacheApiIdempotencyStore` (over `IDistributedCache`) reserves before running, so
duplicates are caught across instances. `IDistributedCache` has no atomic set-if-absent, so
a small cross-node window remains — supply a custom store via `AddApiIdempotency<TStore>()`
(Redis `SET NX` / distributed lock) for a strict guarantee.

---

## Analyzers

### APIEP001 — Handler must return IResult

Enforced on every `MapGet/Post/Put/Delete/Patch/Methods` call inside an `ApiEndpoint`-derived class. Allowed return types: `IResult`, `Task<IResult>`, `ValueTask<IResult>`. Anything else is a compile-time **error**.

**Inline handler — allowed:**

```csharp
public sealed class GetOrderEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("/orders/{orderId:guid}", async (
            Guid orderId,
            IOrderRepository repository,
            CancellationToken cancellationToken
        ) =>
        {
            var order = await repository.GetByIdAsync(orderId, cancellationToken);
            return order is null ? Results.NotFound() : Results.Ok(order);
        })
        .Produces<OrderDetail>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags(EndpointTags.Orders);
}
```

**Separate handler — allowed:**

```csharp
public sealed class CancelOrderEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("/orders/{orderId:guid}/cancel", HandleAsync)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .WithTags(EndpointTags.Orders);

    private async Task<IResult> HandleAsync(
        Guid orderId,
        IOrderRepository repository,
        CancellationToken cancellationToken = default
    )
    {
        var result = await repository.CancelAsync(orderId, cancellationToken);
        return result switch
        {
            CancelResult.NotFound => Results.NotFound(),
            CancelResult.AlreadyShipped => Results.Conflict(),
            _ => Results.NoContent()
        };
    }
}
```

---

## Logging

```csharp
// Program.cs
services.AddApiRequestLog(options =>
{
    options.PropertiesSelector = context => new Dictionary<string, object?>
    {
        ["UserId"] = context.Request.Headers["X-User-Id"].ToString(),
        ["CorrelationId"] = context.Request.Headers["X-Correlation-Id"].ToString()
    };
});
app.UseApiRequestLog();

// Endpoint-specific log properties
app.MapPost("/orders/{orderId}", HandleAsync)
    .WithAdditionalLogProperties(options =>
    {
        options.PropertiesSelector = context => new Dictionary<string, object?>
        {
            ["OrderId"] = context.GetArgument<string>(0)
        };
    });
```
