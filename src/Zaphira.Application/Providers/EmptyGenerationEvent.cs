namespace Zaphira.Application.Providers;

public sealed record EmptyGenerationEvent : ProviderGenerationEvent
{
    public static EmptyGenerationEvent Instance { get; } = new();

    private EmptyGenerationEvent()
    {
    }
}
