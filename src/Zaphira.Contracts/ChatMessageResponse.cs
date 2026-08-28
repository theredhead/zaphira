namespace Zaphira.Contracts;

public sealed record ChatMessageResponse
{
    public ChatMessageResponse(
        Guid id,
        Guid conversationId,
        string role,
        string status,
        IReadOnlyList<MessagePartResponse> parts,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Message id cannot be empty.", nameof(id));
        }

        if (conversationId == Guid.Empty)
        {
            throw new ArgumentException("Conversation id cannot be empty.", nameof(conversationId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        ArgumentNullException.ThrowIfNull(parts);

        MessagePartResponse[] materializedParts = parts.ToArray();
        if (materializedParts.Length == 0)
        {
            throw new ArgumentException("Message response must contain at least one part.", nameof(parts));
        }

        if (materializedParts.Any(part => part is null))
        {
            throw new ArgumentException("Message parts cannot contain null values.", nameof(parts));
        }

        Id = id;
        ConversationId = conversationId;
        Role = role;
        Status = status;
        Parts = materializedParts;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public Guid ConversationId { get; }

    public string Role { get; }

    public string Status { get; }

    public IReadOnlyList<MessagePartResponse> Parts { get; }

    public DateTimeOffset CreatedAt { get; }
}
