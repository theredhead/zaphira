using Zaphira.Domain;

namespace Zaphira.Application.Providers;

public interface IChatModelProvider
{
    ProviderId Id { get; }

    string DisplayName { get; }

    ProviderCapabilities Capabilities { get; }

    Task<ProviderModelCatalog> ListModelsAsync(CancellationToken cancellationToken);

    IAsyncEnumerable<ProviderGenerationEvent> GenerateAsync(
        ProviderGenerationRequest request,
        CancellationToken cancellationToken);
}
