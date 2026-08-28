namespace Zaphira.Contracts;

public sealed record MessageListResponse
{
    public MessageListResponse(IReadOnlyList<ChatMessageResponse> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        ChatMessageResponse[] materializedMessages = messages.ToArray();
        if (materializedMessages.Any(message => message is null))
        {
            throw new ArgumentException("Messages cannot contain null values.", nameof(messages));
        }

        Messages = materializedMessages;
    }

    public IReadOnlyList<ChatMessageResponse> Messages { get; }
}
