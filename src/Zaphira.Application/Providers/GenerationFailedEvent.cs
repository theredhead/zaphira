namespace Zaphira.Application.Providers;

public sealed record GenerationFailedEvent : ProviderGenerationEvent
{
    public GenerationFailedEvent(ProviderError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        Error = error;
    }

    public ProviderError Error { get; }
}
