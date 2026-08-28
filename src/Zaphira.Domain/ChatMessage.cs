namespace Zaphira.Domain;

public sealed record ChatMessage
{
    public ChatMessage(
        MessageId id,
        ConversationId conversationId,
        MessageRole role,
        IEnumerable<IMessagePart> parts,
        MessageStatus status,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(parts);

        IMessagePart[] materializedParts = parts.ToArray();
        if (materializedParts.Length == 0 && status == MessageStatus.Completed)
        {
            throw new ArgumentException("A completed message must contain at least one part.", nameof(parts));
        }

        if (materializedParts.Any(part => part is null))
        {
            throw new ArgumentException("Message parts cannot contain null values.", nameof(parts));
        }

        Id = id;
        ConversationId = conversationId;
        Role = role;
        Parts = materializedParts;
        Status = status;
        CreatedAt = createdAt;
    }

    public MessageId Id { get; }

    public ConversationId ConversationId { get; }

    public MessageRole Role { get; }

    public IReadOnlyList<IMessagePart> Parts { get; }

    public MessageStatus Status { get; }

    public DateTimeOffset CreatedAt { get; }
}
