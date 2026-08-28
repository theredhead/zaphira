namespace Zaphira.Contracts;

public sealed record ConversationResponse
{
    public ConversationResponse(Guid id, string title, string preview, int messageCount, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(preview);
        ArgumentOutOfRangeException.ThrowIfNegative(messageCount);

        Id = id;
        Title = title;
        Preview = preview;
        MessageCount = messageCount;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }

    public string Title { get; }

    public string Preview { get; }

    public int MessageCount { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }
}
