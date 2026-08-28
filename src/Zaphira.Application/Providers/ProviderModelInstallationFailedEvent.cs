namespace Zaphira.Application.Providers;

public sealed record ProviderModelInstallationFailedEvent : ProviderModelInstallationEvent
{
    public ProviderModelInstallationFailedEvent(ProviderError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        Error = error;
    }

    public ProviderError Error { get; }
}
