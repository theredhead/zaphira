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

    public static ErrorResponse None() =>
        new("none", "No error.", "No action required.");

    public static ErrorResponse RouteNotFound() =>
        new("route_not_found", "No endpoint matches the request.", "Check the endpoint path and HTTP method.");

    public static ErrorResponse ConversationNotFound() =>
        new("conversation_not_found", "The conversation was not found.", "Select an existing conversation and try again.");

    public static ErrorResponse MessageNotFound() =>
        new("message_not_found", "The message was not found.", "Reload the conversation and try again.");

    public static ErrorResponse ModelNotFound() =>
        new("model_not_found", "The selected model is not available.", "Choose an installed model and try again.");

    public static ErrorResponse ModelOperationFailed() =>
        new("model_operation_failed", "The model operation failed.", "Check disk space, network access, and provider status, then try again.");

    public static ErrorResponse ProviderUnavailable() =>
        new("provider_unavailable", "The model provider is unavailable.", "Start the provider, go online if needed, then try again.");

    public static ErrorResponse CatalogUnavailable() =>
        new("catalog_unavailable", "The model catalog is unavailable.", "Go online and try syncing the catalog again.");

    public static ErrorResponse UnexpectedServerError() =>
        new("unexpected_server_error", "The server hit an unexpected error.", "Try again.");
}
