namespace Zaphira.Contracts;

public sealed record GenerationStreamResponse
{
    public GenerationStreamResponse(string kind, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentNullException.ThrowIfNull(text);

        Kind = kind;
        Text = text;
    }

    public string Kind { get; }

    public string Text { get; }

    public static GenerationStreamResponse TextDelta(string text) => new("text_delta", text);

    public static GenerationStreamResponse Completed() => new("completed", string.Empty);

    public static GenerationStreamResponse Failed(string text) => new("failed", text);

    public static GenerationStreamResponse Cancelled() => new("cancelled", string.Empty);
}
