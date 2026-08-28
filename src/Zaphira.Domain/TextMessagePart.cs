namespace Zaphira.Domain;

public sealed record TextMessagePart : IMessagePart
{
    public TextMessagePart(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        Text = text;
    }

    public string Text { get; }
}
