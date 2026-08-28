namespace Zaphira.Contracts;

public sealed record ErrorResponse
{
    public ErrorResponse(string code, string message, string suggestion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(suggestion);

        Code = code;
        Message = message;
        Suggestion = suggestion;
    }

    public string Code { get; }

    public string Message { get; }

    public string Suggestion { get; }

    public static ErrorResponse RouteNotFound() =>
        new("route_not_found", "No endpoint matches the request.", "Check the endpoint path and HTTP method.");

    public static ErrorResponse UnexpectedServerError() =>
        new("unexpected_server_error", "The server hit an unexpected error.", "Try again.");
}
