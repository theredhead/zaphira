namespace Zaphira.Application.Providers;

public sealed record GenerationCompletedEvent : ProviderGenerationEvent
{
    public static GenerationCompletedEvent Instance { get; } = new();

    private GenerationCompletedEvent()
    {
    }
}
