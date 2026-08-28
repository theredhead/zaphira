namespace Zaphira.Domain;

public sealed record ConversationSummary
{
    public ConversationSummary(
        ConversationId id,
        string title,
        ConversationPreview preview,
        int messageCount,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(preview);
        ArgumentOutOfRangeException.ThrowIfNegative(messageCount);

        if (updatedAt < createdAt)
        {
            throw new ArgumentException("Updated timestamp cannot be before created timestamp.", nameof(updatedAt));
        }

        Id = id;
        Title = title;
        Preview = preview;
        MessageCount = messageCount;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public ConversationId Id { get; }

    public string Title { get; }

    public ConversationPreview Preview { get; }

    public int MessageCount { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }
}
