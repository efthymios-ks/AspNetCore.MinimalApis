# .NET API Hosting Patterns

## Minimal API

### Registration

```CSharp
var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddApiEndpoints(); 
services.AddSwaggerGen(options => 
{
    options.ConfigureApiEndpoints();
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
await app.UseApiEndpointsAsync(options =>
{
    options.RoutePrefix = "api";
    options.AddGlobalEndpointFilter<CustomGlobalFilter>();
    options.AddGlobalEndpointFilter((context, next) => 
    {
        // Custom filter logic
        return next;
    });
});

await app.RunAsync();
```

### Endpoints

```CSharp
public sealed class CreateProductEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        => endpointRouteBuilder.MapPost("/products", HandleAsync)
            .Accepts<CreateProductRequest>(MediaTypeNames.Application.Json)
            .Produces<Product>(StatusCodes.Status200OK)
            .WithTags(EndpointTags.Products);

    private async Task<IResult> HandleAsync(
        CreateProductRequest request,
        IProductRepository repository,
        CancellationToken cancellationToken = default
    )
    {
        var productToCreate = new Product 
        { 
            Name = request.Name, 
            Price = request.Price 
        };

        var productCreated = await repository.CreateAsync(product, cancellationToken);
        return Results.Ok(productCreated);
    }
}
```

### Analyzers

#### APIEP001 — Handler must return IResult

Enforced on every `MapGet/Post/Put/Delete/Patch/Methods` call inside an `ApiEndpoint`-derived class. Allowed return types: `IResult`, `Task<IResult>`, `ValueTask<IResult>`. Anything else is a compile-time **error**.

**Inline handler — allowed:**

```CSharp
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

```CSharp
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

### Summary

Use `AddParameterExample` to set inline example values for individual route or query parameters. The parameter name matches the DTO property name. Use `nameof` to keep it refactor-safe:

```CSharp
// Single DTO — CategoryId binds from route, Search binds from query
public sealed class SearchProductRequest
{
    public int CategoryId { get; set; }
    public string? Search { get; set; }
}

public sealed class SearchProductEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        => endpointRouteBuilder.MapGet($"/products/categories/{{{nameof(SearchProductRequest.CategoryId)}}}", HandleAsync)
            .Produces<Product[]>(StatusCodes.Status200OK)
            .WithTags(EndpointTags.Products);

    private async Task<IResult> HandleAsync(
        [AsParameters] SearchProductRequest request,
        IProductRepository repository,
        CancellationToken cancellationToken = default
    )
    {
    }
}

public sealed class SearchProductSummary
    : ApiSummary<SearchProductEndpoint>
{
    public SearchProductSummary()
    {
        Summary = "Search products by category";
        Description = Summary;

        AddParameterExample(nameof(SearchProductRequest.CategoryId), 5);
        AddParameterExample(nameof(SearchProductRequest.Search), "Smartphone");
    }
}
```

Use `AddBodyExample` to add named examples to the request body (shown in the Swagger UI dropdown):

```CSharp
public sealed class CreateProductSummary
    : ApiSummary<CreateProductEndpoint
{
    public CreateProductSummary()
    {
        Summary = "Creates a new product";
        Description = Summary;

        AddBodyExample("Simple request", new CreateProductRequest
        {
            Name = "Smartphone",
            Price = 499.99m
        });
    }
}
```

### Custom Swagger Parameters

#### Headers

##### Key-Value
```CSharp
[ApiHeader("X-Api-Key")]
[ApiHeader("X-Correlation-Id", "00000000-0000-0000-0000-000000000000")]
public sealed class GetProductsEndpoint : ApiEndpoint;
```

##### Dropdown

```CSharp
public sealed class LanguageHeaderAttribute
    : ApiHeaderDropdownAttributeBase
{
    public override string Key { get; } = "X-Language";
    public override IEnumerable<string> Values { get; } = ["en", "el", "de"];
    public override string? DefaultValue { get; } = "en";
    public override bool IsRequired { get; } = false;
}

[LanguageHeader]
public sealed class GetProductsEndpoint : ApiEndpoint;
```

#### Query Parameters

##### Key-Value
```CSharp
[ApiQuery("search")]
[ApiQuery("status", "active")]
[ApiQuery("includeDeleted", "false", isRequired: false)]
public sealed class GetProductsEndpoint : ApiEndpoint;
```

##### Dropdown

```CSharp
public sealed class SortOrderQueryAttribute
    : ApiQueryDropdownAttributeBase
{
    public override string Key { get; } = "sortOrder";
    public override IEnumerable<string> Values { get; } = ["asc", "desc"];
    public override string? DefaultValue { get; } = "asc";
    public override bool IsRequired { get; } = false;
}

[SortOrderQuery]
public sealed class GetProductsEndpoint : ApiEndpoint;
```

##### Dynamic Configuration with ConfigureAsync

For scenarios where parameter values need to be loaded from configuration, environment, or services at runtime:

```CSharp
public sealed class RegionQueryAttribute
    : ApiQueryDropdownAttributeBase
{
    private static string _key = null!;
    private static string[] _values = null!;
    private static string _defaultValue = null!;

    // Live (expression-bodied) properties — read the values set in ConfigureAsync.
    // Do NOT auto-initialize ({ get; } = _key;): that captures null before ConfigureAsync runs.
    public override string Key => _key;
    public override IEnumerable<string> Values => _values;
    public override string? DefaultValue => _defaultValue;
    public override bool IsRequired => false;

    // ConfigureAsync runs at most once per attribute type — the framework guards it,
    // so no manual _isConfigured flag (and no thread-safety concern) is needed.
    public override Task ConfigureAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IWebHostEnvironment environment
    )
    {
        var section = configuration.GetSection("Regions");

        _key = section.GetValue<string>("QueryKey")
            ?? throw new InvalidOperationException("QueryKey is not configured.");

        _values = section.GetSection("Available").Get<string[]>()
            ?? throw new InvalidOperationException("Available regions are not configured.");

        _defaultValue = section.GetValue<string>("Default")
            ?? throw new InvalidOperationException("Default region is not configured.");

        return Task.CompletedTask;
    }
}

[RegionQuery]
public sealed class GetProductsEndpoint : ApiEndpoint;
```

**appsettings.json:**
```json
{
  "Regions": {
    "QueryKey": "region",
    "Available": ["us-east", "us-west", "eu-central", "ap-southeast"],
    "Default": "us-east"
  }
}
```

### Validations

```CSharp
public sealed class CreateProductRequestValidator 
    : ApiValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.Price)
            .Positive();
    }
}
```

### Versioning

```CSharp
namespace Samples.MinimalApis.Endpoints.CreateProduct;

// /api/v1/products
[ApiVersionGroup("create-product")]
[ApiVersion(1, Deprecated = true)]
public sealed class CreateProductEndpoint : ApiEndpoint;
```

```CSharp
namespace Samples.MinimalApis.Endpoints.CreateProductV2;

// /api/v2/products
[ApiVersionGroup("create-product")]
[ApiVersion(2)]
public sealed class CreateProductEndpoint : ApiEndpoint;
```

### Idempotency

Idempotency applies to `POST` endpoints. The first request runs the handler and its
**successful** (`2xx`) response is stored; subsequent requests with the same key **replay
that stored response** (same status code and body). While the first request is still in
flight, duplicates get `409 Conflict`. A request that fails (non-`2xx`) or throws releases
the reservation so the client is free to retry.

A **non-empty idempotency key is required** — if `KeySuffixFactory` resolves to an empty
value (e.g. a missing `Idempotency-Key` header), the request is rejected with `400 Bad
Request`. This prevents unrelated requests from colliding on a shared key.

> **Handlers must return an `IResult`.** The stored/replayed response is reconstructed from
> the `IResult` (status code, content type, value), so an idempotent handler must return one
> (e.g. `Results.Ok(...)`, `TypedResults.Created(...)`). Returning a raw `string`, `int`, or
> DTO throws `InvalidOperationException` — it carries no status/content-type to capture.

```CSharp
// Program.cs
services.AddApiIdempotency(options =>
{
    // Global configuration — derive the key from the client-supplied header.
    options.KeySuffixFactory = context
        => context.HttpContext.Request.Headers["Idempotency-Key"].ToString();

    // How long a completed response is replayed for duplicates.
    options.CacheDuration = TimeSpan.FromMinutes(5);

    // Lifetime of the in-flight reservation (should exceed the slowest handler).
    options.ProcessingTimeout = TimeSpan.FromSeconds(30);
});

// Endpoint
public sealed class CreateProductEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        => endpointRouteBuilder.MapPost("/products", HandleAsync)
            .WithApiIdempotency(options =>
            {
                // Endpoint-specific configuration
                options.KeySuffixFactory = context
                    => context.GetArgument<int>(0).ToString();

                options.CacheDuration = TimeSpan.FromMinutes(5);
            });
}
```

> **Atomicity:** the default `DistributedCacheApiIdempotencyStore` (over `IDistributedCache`) reserves the
> key *before* running the handler, so duplicates are caught across instances. Because
> `IDistributedCache` has no atomic set-if-absent, a small cross-node race remains; for a
> strict guarantee supply a custom store via `AddApiIdempotency<TStore>()` backed by an
> atomic primitive (e.g. Redis `SET NX`) or a distributed lock.

### Caching

> **Handlers must return an `IResult`.** As with idempotency, the cached response is
> reconstructed from the `IResult`, so a cached handler must return one (e.g. `Results.Ok(...)`).
> Returning a raw `string`, `int`, or DTO throws `InvalidOperationException`. (Endpoints
> *without* caching or idempotency may return any type — minimal APIs serialize it normally.)

#### Local Caching

```CSharp
using Microsoft.AspNetCore.MinimalApis.ApiCaching.Local;

// Program.cs
services.AddLocalApiCaching(options =>
{
    // Global configuration
    options.KeySuffixFactory = context
        => context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    options.CacheDuration = TimeSpan.FromMinutes(30);
});

// Endpoint
public sealed class GetProductsEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        => endpointRouteBuilder.MapGet("/products", HandleAsync)
            .WithLocalApiCaching(options =>
            {
                // Endpoint-specific configuration
                options.KeySuffixFactory = context
                    => context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                options.CacheDuration = TimeSpan.FromHours(1);
            });
}
```

#### Distributed Caching

```CSharp
using Microsoft.AspNetCore.MinimalApis.ApiCaching.Distributed;

// Program.cs

// Register your distributed cache implementation first
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

// Or use in-memory distributed cache for testing
// services.AddDistributedMemoryCache();

// Then add distributed API caching
services.AddDistributedApiCaching(options =>
{
    // Global configuration
    options.KeySuffixFactory = context
        => context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    options.CacheDuration = TimeSpan.FromMinutes(30);
});

// Endpoint
public sealed class GetProductsEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        => endpointRouteBuilder.MapGet("/products", HandleAsync)
            .WithDistributedApiCaching(options =>
            {
                // Endpoint-specific configuration
                options.KeySuffixFactory = context
                    => context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                options.CacheDuration = TimeSpan.FromHours(1);
            });
}
```

### Logging

#### Request Logging Middleware

Logs all incoming HTTP requests automatically with request/response details, duration, connection info, and errors.

```CSharp
// Program.cs
services.AddApiRequestLog(options =>
{
    // Global properties added to all request logs
    options.PropertiesSelector = context
        => new Dictionary<string, object?>
        {
            ["ApplicationName"] = "MyApp",
            ["Environment"] = builder.Environment.EnvironmentName,
            ["UserId"] = context.Request.Headers["X-User-Id"].ToString(),
            ["CorrelationId"] = context.Request.Headers["X-Correlation-Id"].ToString()
        };
});

var app = builder.Build();
app.UseApiRequestLog(); // Add middleware to pipeline
```

#### Additional Log Properties (Endpoint-Specific)

Add custom properties to specific endpoints that will be included in the request log.

```CSharp
public sealed class CreateOrderEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        => endpointRouteBuilder.MapPost("/orders/{orderId}", HandleAsync)
            .WithAdditionalLogProperties(options =>
            {
                options.PropertiesSelector = context =>
                {
                    // Access raw HTTP request
                    var request = context.HttpContext.Request;
                    
                    // Access bound arguments (deserialized DTOs, route values, etc.)
                    var orderId = context.GetArgument<string>(0); // Route parameter
                    var requestDto = context.GetArgument<CreateOrderRequest>(1); // Body
                    
                    return new Dictionary<string, object?>
                    {
                        ["OrderId"] = orderId,
                        ["CustomerId"] = requestDto.CustomerId,
                        ["OrderType"] = requestDto.OrderType
                    };
                };
            });

    private async Task<IResult> HandleAsync(
        string orderId,
        CreateOrderRequest request,
        IOrderRepository repository,
        CancellationToken cancellationToken = default
    )
    {
        var order = await repository.CreateAsync(orderId, request, cancellationToken);
        return Results.Ok(order);
    }
}
```

### Broadcast (fan-out) + SSE

`IBroadcaster` is a transport-agnostic, strongly-typed **fan-out** bus: every subscriber on a
topic receives every message (pub/sub, **not** a queue). `Stream<T>` returns a live
`IAsyncEnumerable<T>`; `Broadcast<T>` publishes. SSE framing itself is the framework's job —
the endpoint just hands the stream to .NET 10's `TypedResults.ServerSentEvents`.

The only swappable piece is the **transport** (`IBroadcastTransport`):
- `AddBroadcaster()` — in-process (single process / dev).
- `AddRedisBroadcaster("orders:")` — Redis pub/sub for **multi-pod** fan-out (needs an `IConnectionMultiplexer` registered by your app; the optional prefix lets one Redis serve many apps).
- `AddBroadcaster<TTransport>()` — any custom transport.

Both register `TimeProvider.System` (used by polling).

Subscriptions are **multiplexed per pod per topic**: the first `Stream<T>` for a topic opens
one underlying subscription (for Redis, a single channel + handler), additional streams fan out
from it, and the last to leave closes it. So 1000 concurrent streams of one topic on a pod cost
**one** Redis subscription, not 1000.

```CSharp
// Program.cs
services.AddBroadcaster();                 // in-process
// services.AddRedisBroadcaster("orders:"); // multi-pod
```

```CSharp
public sealed class StreamOrdersEndpoint : ApiEndpoint
{
    public const string Topic = "orders";

    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        => endpointRouteBuilder.MapGet("/orders/feed", HandleAsync).WithTags(EndpointTags.Orders);

    private static IResult HandleAsync(IBroadcaster broadcaster, TimeProvider time, CancellationToken cancellationToken)
    {
        var poll = time.Poll(Fetch, every: TimeSpan.FromSeconds(5), fireImmediately: true, cancellationToken: cancellationToken);
        var live = broadcaster.Stream<OrderUpdate>(Topic, cancellationToken);
        return TypedResults.ServerSentEvents(poll.Merge(live, cancellationToken), eventType: "order-update");
    }

    private static Task<IEnumerable<OrderUpdate>> Fetch(CancellationToken cancellationToken) => /* DB read */ ...;
}
```

```CSharp
public sealed class ShipOrderEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        => endpointRouteBuilder.MapPost("/orders/{orderId}/ship", HandleAsync).WithTags(EndpointTags.Orders);

    private static async Task<IResult> HandleAsync(int orderId, IBroadcaster broadcaster, CancellationToken cancellationToken)
    {
        await broadcaster.Broadcast(StreamOrdersEndpoint.Topic, new OrderUpdate(orderId, "Shipped"), cancellationToken);
        return Results.Accepted();
    }
}
```

**Sources are plain `IAsyncEnumerable<T>`**, composed at the endpoint via `StreamExtensions`:
- `ToAsyncEnumerable()` — wrap an `IEnumerable<T>` (e.g. a one-shot DB read).
- `Merge(other)` — run two streams in **parallel** (catch-up poll + live tail).
- `time.Poll(fetch, every, fireImmediately)` / `time.Pulse(period)` — `TimeProvider`-based polling (testable via a substituted `TimeProvider`).

A browser consumes it with `EventSource`:

```js
const source = new EventSource("/api/orders/feed");
source.addEventListener("order-update", (e) => console.log(JSON.parse(e.data)));
```

> **⚠️ Response body must not be buffered.** SSE delivers frames incrementally by flushing the
> response as it streams. Any middleware that **swaps `HttpContext.Response.Body`** for a
> buffering stream (e.g. a request-logging middleware that captures the response into a
> `MemoryStream` and copies it back at the end) will trap every frame in that buffer — nothing
> reaches the client until the connection closes, so the stream appears dead (no polled or live
> events). If you capture the response body, **tee** it: write straight through to the original
> body (so flushes reach the client) and keep only a bounded copy for logging. This repo's
> `LogRequestMiddleware` already does this; apply the same rule to any custom middleware.

### Views

Razor Pages views live under `Samples.MinimalApis/Views/`. Each view is a self-contained folder with four co-located files.

#### Registration

Views are registered and wired up via `Views/DependencyInjection.cs`:

```CSharp
// Program.cs
services.AddViews();

// ...

app.UseViews();
```

`AddViews` registers Razor Pages with the root directory set to `/Views`.
`UseViews` enables static files — global assets from `wwwroot/` and co-located CSS/JS from `Views/` (served at `/views/...`) — and maps Razor Pages routes.

#### Folder structure

```
wwwroot/
└── site.css                     # Global styles (served at /site.css)
Views/
├── DependencyInjection.cs       # AddViews() / UseViews() extension methods
├── _ViewStart.cshtml            # Sets _Layout as the default layout for all views
├── Shared/
│   └── _Layout.cshtml           # Shared HTML shell (links site.css)
└── ProductList/
    ├── ProductList.cshtml       # Razor Page markup  (route: /products)
    ├── ProductList.cshtml.cs    # Page model (code-behind)
    ├── ProductList.css          # Page-scoped styles  (served at /views/ProductList/ProductList.css)
    └── ProductList.js           # Page-scoped script  (served at /views/ProductList/ProductList.js)
```

#### Adding a new view

1. Create a new folder under `Views/` named after the view (e.g. `Views/OrderSummary/`).
2. Add the four files following the same naming convention as `ProductList`.
3. Set an explicit route and link the co-located assets directly in the `.cshtml`:
   ```html
   @page "/order-summary"

   <link rel="stylesheet" href="/views/OrderSummary/OrderSummary.css" />

   <!-- page content -->

   <script src="/views/OrderSummary/OrderSummary.js"></script>
   ```
4. Add any global styles to `wwwroot/site.css`; they are automatically included via `_Layout.cshtml`.

### Testing

`ApiEndpointContext.Create<TEndpoint>` wires an endpoint against an in-memory route builder and returns an `ApiEndpointContext` — no web host required. `ApiEndpointContext` implements `IAsyncDisposable`; use `await using` so the DI container is cleaned up after each test.

#### Setup

Reference `Microsoft.AspNetCore.MinimalApis.Testing` in your test project.

#### Usage

```CSharp
await using var context = ApiEndpointContext.Create<CreateProductEndpoint(services =>
{
    services.AddSingleton<IProductRepository, InMemoryProductRepository>();
});
```

#### ApiEndpointContext

| Member | Type | Description |
|---|---|---|
| `HttpContext` | `DefaultHttpContext` | The live HTTP context. Mutate request headers, body, or path before calling `InvokeAsync`. |
| `Metadata` | `ApiMetadata` | Endpoint metadata extracted at build time (see below). |
| `InvokeAsync()` | `Task<object?>` | Executes the endpoint handler and returns the raw value returned by the handler (`IResult`, plain value, or `EmptyHttpResult` for void). Each call resets the response body. |
| `WithRouteValue(key, value)` | `ApiEndpointContext` | Sets a route value. Returns `this` for chaining. |
| `WithRouteValues(values)` | `ApiEndpointContext` | Sets multiple route values. Returns `this` for chaining. |
| `WithQueryParam(key, value)` | `ApiEndpointContext` | Sets a request query parameter. Returns `this` for chaining. |
| `WithQueryParams(values)` | `ApiEndpointContext` | Sets multiple request query parameters. Returns `this` for chaining. |
| `WithHeader(key, value)` | `ApiEndpointContext` | Sets a request header. Returns `this` for chaining. |
| `WithHeaders(values)` | `ApiEndpointContext` | Sets multiple request headers. Returns `this` for chaining. |
| `WithJsonBody<TElement>(body, options?)` | `ApiEndpointContext` | Serializes `body` as JSON, sets `Body`, `BodyReader`, `Content-Type: application/json`, and `Content-Length`. Returns `this` for chaining. |
| `WithXmlBody<TElement>(body, settings?)` | `ApiEndpointContext` | Serializes `body` as XML, sets `Body`, `Content-Type: application/xml`, and `Content-Length`. Returns `this` for chaining. |
| `WithFormField(key, value)` | `ApiEndpointContext` | Sets a form field. Also sets `Content-Type: application/x-www-form-urlencoded` if not already set. Returns `this` for chaining. |
| `WithFormFields(values)` | `ApiEndpointContext` | Sets multiple form fields. Returns `this` for chaining. |
| `WithFormFile(fieldName, content, fileName, contentType)` | `ApiEndpointContext` | Adds a file to the multipart form. Sets `Content-Type: multipart/form-data`. Returns `this` for chaining. |
| `WithCancellationToken(cancellationToken)` | `ApiEndpointContext` | Sets the request cancellation token. Returns `this` for chaining. |
| `DisposeAsync()` | `ValueTask` | Disposes internal resources (response body, DI container). Always use `await using`. |

#### ApiMetadata

| Property | Type | Description |
|---|---|---|
| `Route` | `string` | The raw route pattern, e.g. `"/products"`. |
| `HttpMethod` | `string` | HTTP method the endpoint responds to, e.g. `"POST"`. |
| `RequiresAuthorization` | `bool` | `true` when `IAuthorizeData` is present and `IAllowAnonymous` is absent. |
| `Metadata` | `IReadOnlyList<object>` | Full endpoint metadata collection (produces, tags, filters, etc.). |
| `Version` | `ApiVersionInfo?` | Version info (`Group`, `Version`, `IsDeprecated`) when both `[ApiVersionGroup]` and `[ApiVersion]` are present; otherwise `null`. |
| `ClassAttributes` | `IReadOnlyList<Attribute>` | Attributes declared directly on the endpoint class. |

#### Example

```CSharp
[Fact]
public async Task CreateProduct_WhenRequestIsValid_ShouldReturnOk()
{
    // Arrange
    await using var context = ApiEndpointContext.Create<CreateProductEndpoint>(services =>
    {
        services.AddSingleton<IProductRepository, InMemoryProductRepository>();
    });
     
    context.WithHeader("X-Api-Key", "my-api-key");
    await context.WithJsonBody(new CreateProductRequest
    {
        Name = "Test Product",
        Price = 9.99m
    });

    // Act
    var result = await context.InvokeAsync();

    // Assert
    Assert.Equal("/products", context.Metadata.Route);
    Assert.Equal(HttpMethods.Post, context.Metadata.HttpMethod);

    var statusCodeResult = Assert.IsType<IStatusCodeHttpResult>(result, exactMatch: false);
    Assert.Equal(StatusCodes.Status200OK, statusCodeResult.StatusCode);

    var apiKey = context.HttpContext.Request.Headers["X-Api-Key"].ToString();
    Assert.Equal("my-api-key", apiKey);

    var product = await context.ReadJsonBodyAsync<Product>();
    Assert.Equal("Test Product", product.Name);
    Assert.Equal(9.99m, product.Price);
}
```

---

## FastEndpoints

### Registration

```CSharp
var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddFastEndpoints()
    .SwaggerDocument(options =>
    {
        options.DocumentSettings = settings =>
        {
            settings.DocumentName = "Initial Release";
            settings.Title = "My API";
            settings.Version = "v0";
        };
    })
    .SwaggerDocument(options =>
    {
        options.MaxEndpointVersion = 1;
        options.DocumentSettings = settings =>
        {
            settings.DocumentName = "Release 1";


            settings.Title = "My API";
            settings.Version = "v1";
        };
    })
    .SwaggerDocument(options =>
    {
        options.MaxEndpointVersion = 2;
        options.DocumentSettings = settings =>
        {
            settings.DocumentName = "Release 2";
            settings.Title = "My API";
            settings.Version = "v2";
        };
    });

var app = builder.Build();
app.UseFastEndpoints(config =>
{
    config.Endpoints.RoutePrefix = "api";
    config.Versioning.Prefix = "v";
    config.Versioning.PrependToRoute = true;
}).UseSwaggerGen();

await app.RunAsync();
```

### Endpoints

```CSharp
public class CreateProductEndpoint(IProductRepository repository)
    : Endpoint<CreateProductRequest, Product>
{
    public override void Configure()
    {
        Post("/products");
        AllowAnonymous();

        Description(options => options
            .Produces<Product>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
        );
    }

    public override async Task HandleAsync(CreateProductRequest req, CancellationToken cancellationToken)
    {
        var productToCreate = new Product 
        { 
            Name = req.Name, 
            Price = req.Price 
        };

        var producCreated = await repository.CreateAsync(product, cancellationToken);
        await SendOkAsync(producCreated, cancellationToken);
    }
}
```

### Descriptors

```CSharp
public sealed class CreateProductEndpointSummary 
    : Summary<CreateProductEndpoint>
{
    public CreateProductEndpointSummary()
    {
        Summary = "Create a new product";
        Description = Summary;

        RequestExamples.Add(new(new CreateProductRequest
        {
            Name = "Smartphone",
            Price = 499.99m
        }, "Simple request"));
    }
}
```

### Validations

```CSharp
public sealed class CreateProductRequestValidator 
    : Validator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.Price)
            .GreaterThan(0);
    }
}
```

### Versioning

```CSharp
namespace Samples.FastEndpoints.Endpoints.CreateProduct;

// /api/v1/products
public sealed class CreateProductEndpoint
    : Endpoint<CreateProductRequest, Product>
{
    public override void Configure()
    {
        Post("/products");
        Version(1, deprecateAt: 2);
    }
}
```

```CSharp
namespace Samples.FastEndpoints.Endpoints.CreateProductV2;

// /api/v2/products
public sealed class CreateProductEndpoint
    : Endpoint<CreateProductRequest, Product>
{
    public override void Configure()
    {
        Post("/products");
        Version(2);
    }
}
```
