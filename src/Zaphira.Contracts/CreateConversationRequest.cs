namespace Zaphira.Contracts;

public sealed record CreateConversationRequest
{
    public CreateConversationRequest(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Title = title;
    }

    public string Title { get; }
}
