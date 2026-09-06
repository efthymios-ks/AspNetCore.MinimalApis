using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Samples.MinimalApis.Views.ViewsIndex;

public sealed class ViewsIndexModel(EndpointDataSource endpointDataSource) : PageModel
{
    public string[] Routes { get; private set; } = [];

    public void OnGet()
    {
        var currentRoute = HttpContext.Request.Path.Value?.Trim('/') ?? string.Empty;

        Routes = [.. endpointDataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Where(pageEndpoint => pageEndpoint.Metadata.GetMetadata<PageActionDescriptor>() is not null)
            .Select(pageEndpoint => pageEndpoint.RoutePattern.RawText ?? string.Empty)
            .Where(route => {
                var cleanRoute = route.Trim('/');
                return !string.IsNullOrWhiteSpace(cleanRoute)
                    && !string.Equals(cleanRoute, currentRoute, StringComparison.OrdinalIgnoreCase);
            })
            .Order()];
    }
}
