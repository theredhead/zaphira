namespace Zaphira.Application.Providers;

public sealed record TextGenerationDeltaEvent : ProviderGenerationEvent
{
    public TextGenerationDeltaEvent(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        Text = text;
    }

    public string Text { get; }
}
