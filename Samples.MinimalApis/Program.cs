using Microsoft.AspNetCore.MinimalApis.ApiCaching.Distributed;
using Microsoft.AspNetCore.MinimalApis.ApiCaching.Local;
using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;
using Microsoft.AspNetCore.MinimalApis.ApiIdempotency;
using Microsoft.AspNetCore.MinimalApis.Broadcast;
using Microsoft.AspNetCore.MinimalApis.ApiLogging;
using Microsoft.AspNetCore.MinimalApis.ApiSwagger;
using Samples.Shared.Repositories;
using System.Text.Json;
using System.Text.Json.Serialization;
using Samples.MinimalApis.Views;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddSingleton<IProductRepository, InMemoryProductRepository>();
services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

services.AddLocalApiCaching();

services.AddDistributedMemoryCache();
services.AddDistributedApiCaching();

services.AddApiIdempotency(options => options.KeySuffixFactory = context
    => context.HttpContext.Request.Headers["Idempotency-Key"].ToString());

services.AddApiRequestLog(options => options.PropertiesSelector = context
    => new Dictionary<string, object?>
    {
        ["Environment"] = builder.Environment.EnvironmentName,
        ["MachineName"] = Environment.MachineName,
        ["App"] = "Minimal APIs App",
    });

services.AddBroadcaster();

services.AddViews();
services.AddApiEndpoints();
services.AddSwaggerGen(options =>
{
    options.ConfigureApiEndpoints();
    options.CustomSchemaIds(type => type.FullName);
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.DisplayOperationId();
    options.DisplayRequestDuration();
    options.EnableDeepLinking();
    options.EnableTryItOutByDefault();
    options.EnableFilter();
    options.InjectJavascript("/swagger-default-theme.js");
    options.InjectJavascript("/swagger-views-index.js");
    options.InjectJavascript("/swagger-html-preview.js");
});

app.UseApiRequestLog();
await app.UseApiEndpointsAsync(options => options.RoutePrefix = "/api");
app.UseViews();

await app.RunAsync();
