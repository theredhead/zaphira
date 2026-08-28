using Zaphira.Contracts;

namespace Zaphira.Client.Chat;

public sealed class ChatApiException : Exception
{
    public ChatApiException(int httpStatusCode, ErrorResponse error)
        : base($"{error.Message} {error.Suggestion}")
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentOutOfRangeException.ThrowIfNegative(httpStatusCode);

        HttpStatusCode = httpStatusCode;
        Error = error;
    }

    public int HttpStatusCode { get; }

    public ErrorResponse Error { get; }

    public static ChatApiException None { get; } =
        new(0, new ErrorResponse("none", "No error.", "No action required."));
}
