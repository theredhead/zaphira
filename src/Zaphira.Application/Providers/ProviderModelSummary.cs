using Zaphira.Domain;

namespace Zaphira.Application.Providers;

public sealed record ProviderModelSummary
{
    public ProviderModelSummary(ModelId id, string displayName, ProviderCapabilities capabilities)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(capabilities);

        Id = id;
        DisplayName = displayName;
        Capabilities = capabilities;
    }

    public ModelId Id { get; }

    public string DisplayName { get; }

    public ProviderCapabilities Capabilities { get; }
}
