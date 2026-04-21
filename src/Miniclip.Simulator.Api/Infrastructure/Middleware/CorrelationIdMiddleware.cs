using Miniclip.Core.Messaging;

namespace Miniclip.Simulator.Api.Infrastructure.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext httpContext, IMutablePropagationContext propagationContext)
    {
        var correlationId = httpContext.Request.Headers.TryGetValue(HeaderName, out var value)
            && Guid.TryParse(value, out var parsed)
                ? parsed
                : Guid.NewGuid();

        propagationContext.CorrelationId = correlationId;
        propagationContext.CausationId = correlationId;

        httpContext.Response.OnStarting(() =>
        {
            httpContext.Response.Headers[HeaderName] = propagationContext.CorrelationId.ToString();
            return Task.CompletedTask;
        });

        await next(httpContext);
    }
}
