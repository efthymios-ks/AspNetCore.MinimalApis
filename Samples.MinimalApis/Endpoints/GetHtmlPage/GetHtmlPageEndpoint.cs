using Microsoft.AspNetCore.MinimalApis.ApiEndpoints;
using System.Net.Mime;

namespace Samples.MinimalApis.Endpoints.GetHtmlPage;

public sealed class GetHtmlPageEndpoint : ApiEndpoint
{
    public override RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        => endpointRouteBuilder.MapGet("/html-page", Handle)
            .Produces<string>(StatusCodes.Status200OK, MediaTypeNames.Text.Html)
            .WithTags("Html");

    private static IResult Handle() => Results.Content("""
    <!DOCTYPE html>
    <html lang="en">
    <head>
        <meta charset="UTF-8" />
        <title>Dummy Page</title>
        <style>
            *, *::before, *::after { box-sizing: border-box; }
            body { font-family: sans-serif; max-width: 640px; margin: 3rem auto; padding: 0 1rem; color: #333; }
            h1 { color: #49cc90; }
            .card { border: 1px solid #ddd; border-radius: 6px; padding: 1rem 1.25rem; margin-bottom: 1rem; }
            button { background: #49cc90; color: #fff; border: none; border-radius: 4px; padding: 0.4rem 1rem; font-size: 0.9rem; cursor: pointer; }
            button:hover { background: #3db87a; }
            button.danger { background: #f93e3e; }
            button.danger:hover { background: #d93030; }
            input[type="text"] { border: 1px solid #ccc; border-radius: 4px; padding: 0.375rem 0.625rem; font-size: 0.9rem; width: 100%; margin-bottom: 0.5rem; }
            #counter-value { font-size: 2rem; font-weight: bold; color: #49cc90; display: inline-block; min-width: 3rem; text-align: center; }
            #todo-list { list-style: none; padding: 0; margin: 0.5rem 0; }
            #todo-list li { display: flex; justify-content: space-between; align-items: center; padding: 0.375rem 0; border-bottom: 1px solid #f0f0f0; }
            #todo-list li:last-child { border-bottom: none; }
            #todo-list li button { padding: 0.2rem 0.5rem; font-size: 0.75rem; }
            #alert-box { display: none; background: #fff3cd; border: 1px solid #ffc107; border-radius: 4px; padding: 0.5rem 1rem; margin-top: 0.75rem; }
        </style>
    </head>
    <body>
        <h1>Interactive Dummy Page</h1>
        <p>Served as <code>text/html</code> from the API.</p>

        <div class="card">
            <h3>Counter</h3>
            <button onclick="change(-1)">−</button>
            <span id="counter-value">0</span>
            <button onclick="change(1)">+</button>
            <button class="danger" onclick="reset()" style="margin-left:0.5rem">Reset</button>
        </div>

        <div class="card">
            <h3>To-do List</h3>
            <input type="text" id="todo-input" placeholder="Add an item…" onkeydown="if(event.key==='Enter')addTodo()" />
            <button onclick="addTodo()">Add</button>
            <ul id="todo-list"></ul>
        </div>

        <div class="card">
            <h3>Alert</h3>
            <button onclick="toggleAlert()">Toggle alert</button>
            <div id="alert-box">This is an alert message from the page!</div>
        </div>

        <script>
            let count = 0;
            function change(n) { count += n; document.getElementById('counter-value').textContent = count; }
            function reset() { count = 0; document.getElementById('counter-value').textContent = 0; }

            function addTodo() {
                const input = document.getElementById('todo-input');
                const text = input.value.trim();
                if (!text) return;
                const li = document.createElement('li');
                li.innerHTML = `<span>${text}</span><button class="danger" onclick="this.parentElement.remove()">Remove</button>`;
                document.getElementById('todo-list').appendChild(li);
                input.value = '';
            }

            function toggleAlert() {
                const box = document.getElementById('alert-box');
                box.style.display = box.style.display === 'block' ? 'none' : 'block';
            }
        </script>
    </body>
    </html>
    """, MediaTypeNames.Text.Html);
}
