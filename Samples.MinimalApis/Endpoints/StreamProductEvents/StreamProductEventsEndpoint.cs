using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;
using Microsoft.AspNetCore.MinimalApis.Broadcast;
using System.Net.Mime;

namespace Samples.MinimalApis.Endpoints.StreamProductEvents;

public sealed class StreamProductEventsEndpoint : ApiEndpoint
{
    public const string Topic = "product-events";

    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        => endpointRouteBuilder.MapGet("/products/events", HandleAsync)
            .WithTags(EndpointTags.Products);

    private static IResult HandleAsync(
        HttpContext httpContext,
        IBroadcaster broadcaster,
        TimeProvider time,
        CancellationToken cancellationToken
    )
    {
        var accept = httpContext.Request.Headers.Accept.ToString();
        if (!accept.Contains(MediaTypeNames.Text.EventStream, StringComparison.OrdinalIgnoreCase))
        {
            return Results.Content(HtmlPage, MediaTypeNames.Text.Html);
        }

        var poll = time.Poll(Fetch, every: TimeSpan.FromSeconds(5), fireImmediately: true, cancellationToken: cancellationToken);
        var live = broadcaster.Stream<ProductEvent>(Topic, cancellationToken);

        return TypedResults.ServerSentEvents(poll.Merge(live, cancellationToken), eventType: "product-event");
    }

    // Dummy in-memory poll source: emits one tick every interval.
    private static Task<IEnumerable<ProductEvent>> Fetch(CancellationToken cancellationToken)
        => Task.FromResult<IEnumerable<ProductEvent>>(
            [new ProductEvent(0, $"polled {DateTime.UtcNow:HH:mm:ss}")]);

    private const string HtmlPage =
        """
    <!doctype html>
    <html lang="en">
    <head>
        <meta charset="utf-8" />
        <title>Product Events (SSE)</title>
        <style>
            body { font-family: system-ui, sans-serif; margin: 2rem; }
            button { padding: .5rem 1rem; font-size: 1rem; cursor: pointer; }
            pre { background: #111; color: #0f0; padding: 1rem; height: 60vh; overflow: auto; }
        </style>
    </head>
    <body>
        <h1>Product Events (SSE)</h1>
        <p>Polling pushes every 5s automatically. Click to trigger a manual event.</p>
        <button id="trigger">Trigger event</button>
        <pre id="log"></pre>
        <script>
            const log = document.getElementById("log");
            const append = (line) => {
                log.textContent += line + "\n";
                log.scrollTop = log.scrollHeight;
            };

            const source = new EventSource(window.location.pathname);
            source.addEventListener("product-event", (e) => {
                const message = JSON.parse(e.data);
                const kind = message.status && message.status.startsWith("polled") ? "polled" : "event";
                append(kind + ": " + e.data);
            });
            source.onopen = () => append("[connected]");
            source.onerror = () => append("[disconnected — retrying]");

            document.getElementById("trigger").addEventListener("click", async () => {
                const body = JSON.stringify({
                    productId: Math.floor(Math.random() * 1000),
                    status: "triggered"
                });
                await fetch(window.location.pathname, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body
                });
            });
        </script>
    </body>
    </html>
    """;
}
