using System.Net.Http.Headers;
using Zaphira.Contracts;

namespace Zaphira.Server.Pairing;

internal sealed class PairingAuthorizationMiddleware
{
    private readonly RequestDelegate next;

    public PairingAuthorizationMiddleware(RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(next);

        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context, ServerPairingRegistry pairingRegistry)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pairingRegistry);

        if (IsPairingBootstrapRequest(context)
            || !await pairingRegistry.HasPairingsAsync(context.RequestAborted))
        {
            await next(context);
            return;
        }

        string accessToken = ReadBearerToken(context);
        if (!string.IsNullOrWhiteSpace(accessToken)
            && await pairingRegistry.IsAccessTokenAuthorizedAsync(accessToken, context.RequestAborted))
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(ErrorResponse.PairingRequired(), context.RequestAborted);
    }

    private static bool IsPairingBootstrapRequest(HttpContext context)
    {
        PathString path = context.Request.Path;

        return path.Equals("/health", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/pairing-code", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/pairings", StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadBearerToken(HttpContext context)
    {
        string authorization = context.Request.Headers.Authorization.ToString();
        if (!AuthenticationHeaderValue.TryParse(authorization, out AuthenticationHeaderValue? header)
            || !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(header.Parameter))
        {
            return string.Empty;
        }

        return header.Parameter;
    }
}
