namespace Zaphira.Contracts;

public sealed record MessagePartResponse
{
    public MessagePartResponse(string kind, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentNullException.ThrowIfNull(text);

        Kind = kind;
        Text = text;
    }

    public string Kind { get; }

    public string Text { get; }
}
