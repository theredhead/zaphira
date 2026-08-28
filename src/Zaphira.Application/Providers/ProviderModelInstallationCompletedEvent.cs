namespace Zaphira.Application.Providers;

public sealed record ProviderModelInstallationCompletedEvent : ProviderModelInstallationEvent
{
    private ProviderModelInstallationCompletedEvent()
    {
    }

    public static ProviderModelInstallationCompletedEvent Instance { get; } = new();
}
