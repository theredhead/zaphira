namespace Zaphira.Contracts;

public sealed record UpdateConversationRequest
{
    public UpdateConversationRequest(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Title = title;
    }

    public string Title { get; }
}
