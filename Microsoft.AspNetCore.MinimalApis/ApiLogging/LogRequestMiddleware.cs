using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text;

namespace Microsoft.AspNetCore.MinimalApis.ApiLogging;

internal sealed class LogRequestMiddleware(
    RequestDelegate next,
    ILogger<LogRequestMiddleware> logger,
    IOptions<GlobalLogScopeOptions> globalOptions
)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger _logger = logger;
    private readonly GlobalLogScopeOptions _globalOptions = globalOptions.Value;

    private static readonly IEnumerable<string> _requestHeadersToLog
        = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
        };

    private static readonly IEnumerable<string> _responseHeadersToLog
        = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
        };

    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.EnableBuffering();
        Exception? capturedException = null;
        var capturedResponseBody = string.Empty;

        var stopWatch = Stopwatch.StartNew();
        try
        {
            capturedResponseBody = await RunAndCaptureBodyAsync(context, () => _next(context));
        }
        catch (Exception exception)
        {
            stopWatch.Stop();
            capturedException = exception;
            throw;
        }
        finally
        {
            stopWatch.Stop();
            var logProperties = await GetLogPropertiesAsync(
                context,
                stopWatch.Elapsed,
                capturedResponseBody,
                capturedException
            );

            var logLevel = GetLogLevel(context.Response.StatusCode, capturedException);
            using (_logger.BeginScope(logProperties))
            {
                _logger.Log(logLevel, "Handled incoming HTTP request");
            }
        }
    }

    private static async Task<string> RunAndCaptureBodyAsync(
        HttpContext context,
        Func<Task> next
    )
    {
        var originalBody = context.Response.Body;
        var captureStream = new ResponseCaptureStream(originalBody);
        context.Response.Body = captureStream;

        try
        {
            await next();
            return captureStream.GetCapturedText();
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    /// <summary>
    /// Forwards writes straight to the real response body (so streaming/SSE flushes reach
    /// the client immediately) while keeping a bounded prefix for logging. The previous
    /// approach buffered the whole response in memory, which never flushed a live stream.
    /// </summary>
    /// <param name="inner"></param>
    private sealed class ResponseCaptureStream(Stream inner) : Stream
    {
        private const int MaxCapturedBytes = 32 * 1024;

        private readonly Stream _inner = inner;
        private readonly MemoryStream _captured = new();

        public override bool CanRead
            => false;

        public override bool CanSeek
            => false;

        public override bool CanWrite
            => true;

        public override long Length
            => throw new NotSupportedException();

        public override long Position
        {
            get => 0;
            set { }
        }

        public string GetCapturedText()
            => Encoding.UTF8.GetString(_captured.ToArray());

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            Capture(buffer.AsSpan(offset, count));
            _inner.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Capture(buffer.AsSpan(offset, count));
            return _inner.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Capture(buffer.Span);
            await _inner.WriteAsync(buffer, cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void Flush()
            => _inner.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken)
            => _inner.FlushAsync(cancellationToken);

        private void Capture(ReadOnlySpan<byte> data)
        {
            var remaining = MaxCapturedBytes - (int)_captured.Length;
            if (remaining <= 0)
            {
                return;
            }

            _captured.Write(data[..Math.Min(remaining, data.Length)]);
        }
    }

    private async Task<IReadOnlyDictionary<string, object?>> GetLogPropertiesAsync(
        HttpContext context,
        TimeSpan duration,
        string responseBody,
        Exception? exception
    )
    {
        var httpConnection = context.Connection;
        var httpRequest = context.Request;
        var httpResponse = context.Response;

        var properties = new Dictionary<string, object?>
        {
            [PrefixKey("ElapsedMilliseconds")] = duration.TotalMilliseconds
        };

        AddRequest(properties, httpRequest);
        await AddRequestBodyAsync(properties, httpRequest, context.RequestAborted);
        AddResponse(properties, httpResponse, responseBody);
        AddConnection(properties, httpConnection);
        AddError(properties, exception);
        AddAdditionalProperties(properties, context);

        return properties;
    }

    private static void AddRequest(
        Dictionary<string, object?> properties,
        HttpRequest request
    )
    {
        properties[PrefixKey("Request.Method")] = request.Method;
        properties[PrefixKey("Request.Scheme")] = request.Scheme;
        properties[PrefixKey("Request.Host")] = request.Host.Value;
        properties[PrefixKey("Request.Path")] = request.Path.Value;
        properties[PrefixKey("Request.QueryString")] = request.QueryString.Value;

        var headersToLog = request
            .Headers
            .Where(header => _requestHeadersToLog.Contains(header.Key));
        foreach (var (key, value) in headersToLog)
        {
            properties[PrefixKey($"Request.Header.{key}")] = value.ToString();
        }
    }

    private static async Task AddRequestBodyAsync(
        Dictionary<string, object?> properties,
        HttpRequest request,
        CancellationToken cancellationToken
    )
    {
        var body = request.Body;
        body.Position = 0;
        using var reader = new StreamReader(body, leaveOpen: true);
        var content = await reader.ReadToEndAsync(cancellationToken);
        body.Position = 0;

        properties[PrefixKey("Request.Body")] = content;
    }

    private static void AddResponse(
        Dictionary<string, object?> properties,
        HttpResponse response,
        string responseBody
    )
    {
        properties[PrefixKey("Response.StatusCode")] = response.StatusCode;
        properties[PrefixKey("Response.Body")] = responseBody;

        var headersToLog = response
            .Headers
            .Where(header => _responseHeadersToLog.Contains(header.Key));
        foreach (var (key, value) in headersToLog)
        {
            properties[PrefixKey($"Response.Header.{key}")] = value.ToString();
        }
    }

    private static void AddConnection(
        Dictionary<string, object?> properties,
        ConnectionInfo connection
    )
    {
        properties[PrefixKey("Connection.Id")] = connection.Id;

        if (connection.LocalIpAddress is { } localIpAddress)
        {
            properties[PrefixKey("Connection.LocalIpAddress")] = localIpAddress.ToString();
            properties[PrefixKey("Connection.LocalPort")] = connection.LocalPort;
        }

        if (connection.RemoteIpAddress is { } remoteIpAddress)
        {
            properties[PrefixKey("Connection.RemoteIpAddress")] = remoteIpAddress.ToString();
            properties[PrefixKey("Connection.RemotePort")] = connection.RemotePort;
        }
    }

    private static void AddError(
        Dictionary<string, object?> properties,
        Exception? exception
    )
    {
        if (exception is null)
        {
            return;
        }

        properties[PrefixKey("Exception.Message")] = exception.Message;
        properties[PrefixKey("Exception.StackTrace")] = exception.StackTrace;
    }

    private void AddAdditionalProperties(
        Dictionary<string, object?> properties,
        HttpContext context
    )
    {
        var additionalProperties = new Dictionary<string, object?>();
        var globalProperties = _globalOptions.PropertiesSelector(context);
        foreach (var (key, value) in globalProperties)
        {
            additionalProperties[key] = value;
        }

        if (context.Items.TryGetValue(EndpointLogScopeOptions.Key, out var endpointPropertiesAsObject)
            && endpointPropertiesAsObject is IDictionary<string, object?> endpointProperties
        )
        {
            foreach (var (key, value) in endpointProperties)
            {
                additionalProperties[key] = value;
            }
        }

        if (additionalProperties.Count > 0)
        {
            properties[PrefixKey("AdditionalProperties")] = additionalProperties;
        }
    }

    private static LogLevel GetLogLevel(int statusCode, Exception? exception)
    {
        if (exception is not null)
        {
            return LogLevel.Error;
        }

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            return LogLevel.Error;
        }

        if (statusCode >= StatusCodes.Status400BadRequest)
        {
            return LogLevel.Warning;
        }

        return LogLevel.Information;
    }

    private static string PrefixKey(string key)
        => $"{key}";
}
