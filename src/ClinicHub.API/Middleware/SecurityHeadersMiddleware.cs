namespace ClinicHub.API.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.TryAdd("X-Content-Type-Options", "nosniff");
            headers.TryAdd("X-Frame-Options", "DENY");
            headers.TryAdd("Referrer-Policy", "no-referrer");
            headers.TryAdd("Permissions-Policy", "camera=(), geolocation=(), microphone=()");

            if (!context.Request.Path.StartsWithSegments("/swagger"))
            {
                headers.TryAdd("Content-Security-Policy", "default-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'self'");
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}
