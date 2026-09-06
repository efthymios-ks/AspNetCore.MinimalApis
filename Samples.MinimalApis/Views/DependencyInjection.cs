using Microsoft.Extensions.FileProviders;

namespace Samples.MinimalApis.Views;

public static class DependencyInjection
{
    public static IServiceCollection AddViews(this IServiceCollection services)
    {
        services.AddRazorPages()
            .AddRazorPagesOptions(options => options.RootDirectory = "/Views");

        return services;
    }

    public static WebApplication UseViews(this WebApplication app)
    {
        app.UseStaticFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(
                Path.Combine(app.Environment.ContentRootPath, "Views")),
            RequestPath = "/views"
        });
        app.MapRazorPages();

        return app;
    }
}
