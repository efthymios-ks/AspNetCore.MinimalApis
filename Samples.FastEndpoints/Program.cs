using FastEndpoints;
using FastEndpoints.Swagger;
using Samples.Shared.Repositories;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddSingleton<IProductRepository, InMemoryProductRepository>();
services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

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
