namespace Zaphira.Contracts;

public sealed record SendMessageResponse
{
    public SendMessageResponse(Guid userMessageId, Guid assistantMessageId)
    {
        if (userMessageId == Guid.Empty)
        {
            throw new ArgumentException("User message id cannot be empty.", nameof(userMessageId));
        }

        if (assistantMessageId == Guid.Empty)
        {
            throw new ArgumentException("Assistant message id cannot be empty.", nameof(assistantMessageId));
        }

        UserMessageId = userMessageId;
        AssistantMessageId = assistantMessageId;
    }

    public Guid UserMessageId { get; }

    public Guid AssistantMessageId { get; }
}
