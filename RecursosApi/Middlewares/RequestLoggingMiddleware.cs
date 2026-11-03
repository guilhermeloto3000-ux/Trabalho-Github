using System.Diagnostics;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var cronometro = Stopwatch.StartNew();
        await _next(context);
        cronometro.Stop();

        _logger.LogInformation("{Metodo} {Caminho} respondeu {Status} em {Ms}ms",
            context.Request.Method, context.Request.Path,
            context.Response.StatusCode, cronometro.ElapsedMilliseconds);
    }
}
