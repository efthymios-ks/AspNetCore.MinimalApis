---
name: minimal-apis
description: Microsoft.AspNetCore.MinimalApis library usage guide for .NET Core. Use when creating ApiEndpoint classes, calling AddApiEndpoints or UseApiEndpointsAsync, implementing MapEndpoint with RouteHandlerBuilder, registering endpoints in DI, adding Swagger parameters with ApiHeader or ApiQuery attributes, versioning with ApiVersionGroup and ApiVersion, validating requests with ApiValidator, writing FluentValidation rules for endpoints, testing endpoints with ApiEndpointContext, accessing ApiEndpointContext, ApiMetadata, InvokeAsync, HttpContext, RequiresAuthorization, HttpMethod, Route, ClassAttributes, Version, ApiVersionInfo, using local or distributed API caching, configuring idempotency, adding request logging middleware, or exploring endpoint patterns in this project.
---

# Microsoft.AspNetCore.MinimalApis Usage

## When to Use

- Creating a new API endpoint
- Registering endpoints in Program.cs
- Adding metadata (versioning, auth, swagger params, validation)
- Testing an endpoint in isolation with `ApiEndpointContext.Create`

## Core rules

- **Endpoint** = a `sealed class : ApiEndpoint` overriding `MapEndpoint(IEndpointRouteBuilder)` and returning the
  `RouteHandlerBuilder`. Only **concrete** classes auto-register.
- **DI** goes on the **handler-method parameters**, never a constructor. The handler must return `IResult` /
  `Task<IResult>` / `ValueTask<IResult>` (analyzer **APIEP001**).
- **Register once**: `services.AddApiEndpoints()` + `services.AddSwaggerGen(o => o.ConfigureApiEndpoints())`, then
  `await app.UseApiEndpointsAsync(...)`; set `RoutePrefix` only if you want one.
- **JSON**: set the naming policy **explicitly** on **both** `ConfigureHttpJsonOptions` (runtime) and
  `Microsoft.AspNetCore.Mvc.JsonOptions` (Swagger schema) — don't rely on either default, or the docs drift from
  what the API emits. `ConfigureApiEndpoints()` also normalizes enum path/query params to their exact PascalCase
  names for the case-sensitive binder.
- **Validation**: `ApiValidator<T>` (built-in rule builders), auto-discovered; the `ApiValidatorFilter` runs on
  every endpoint and returns `400` on failure. No per-validator registration.
- **Versioning**: `[ApiVersion(n)]` (major only) + `[ApiVersionGroup("name")]`, both required together; one class
  per version.
- **Swagger metadata**: `[ApiHeader]` / `[ApiQuery]` (+ dropdown bases) for params; `ApiSummary<TEndpoint>` for
  summary/description and request/response/parameter examples.
- **Caching** (local in-memory or distributed), **idempotency**, **request logging**: opt in per route via
  `.WithLocalApiCaching(...)` / `.WithDistributedApiCaching(...)` / `.WithApiIdempotency()`, or wire logging
  middleware. Cached and replayed bodies reuse the host's JSON options.
- **Testing**: `ApiEndpointContext.Create<TEndpoint>(services => …)` runs the real pipeline in-process — register
  every handler dependency (plus a distributed/local cache for a cached endpoint). Validators are **not** auto-wired here.

## Reference

All code, patterns and member tables live in [REFERENCE.md](REFERENCE.md):

- [Registration Options](REFERENCE.md#registration-options) (incl. JSON for runtime + Swagger)
- [Endpoint Patterns](REFERENCE.md#endpoint-patterns)
- [Swagger Parameters](REFERENCE.md#swagger-parameters)
- [Versioning](REFERENCE.md#versioning)
- [Validation](REFERENCE.md#validation)
- [Testing with ApiEndpointContext](REFERENCE.md#testing-with-apiendpointcontext)
- [Caching](REFERENCE.md#caching)
- [Idempotency](REFERENCE.md#idempotency)
- [Analyzers](REFERENCE.md#analyzers)
- [Logging](REFERENCE.md#logging)
