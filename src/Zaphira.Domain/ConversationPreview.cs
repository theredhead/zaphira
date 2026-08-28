namespace Zaphira.Domain;

public sealed record ConversationPreview
{
    private const string EmptyPreviewText = "No messages yet.";

    public ConversationPreview(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        Text = text;
    }

    public string Text { get; }

    public static ConversationPreview Empty() => new(EmptyPreviewText);
}
