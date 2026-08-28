using Zaphira.Domain;

namespace Zaphira.Application.Providers;

public sealed record ProviderModelCatalog
{
    public ProviderModelCatalog(ProviderId providerId, IEnumerable<ProviderModelSummary> models)
    {
        ArgumentNullException.ThrowIfNull(providerId);
        ArgumentNullException.ThrowIfNull(models);

        ProviderModelSummary[] materializedModels = models.ToArray();
        if (materializedModels.Any(model => model is null))
        {
            throw new ArgumentException("Provider models cannot contain null values.", nameof(models));
        }

        ProviderId = providerId;
        Models = materializedModels;
    }

    public ProviderId ProviderId { get; }

    public IReadOnlyList<ProviderModelSummary> Models { get; }
}
