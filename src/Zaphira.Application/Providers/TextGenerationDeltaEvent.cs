namespace Zaphira.Application.Providers;

public sealed record TextGenerationDeltaEvent : ProviderGenerationEvent
{
    public TextGenerationDeltaEvent(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        Text = text;
    }

    public string Text { get; }
}
