using Zaphira.Domain;

namespace Zaphira.Application.Providers;

public interface IChatModelProvider
{
    ProviderId Id { get; }

    string DisplayName { get; }

    ProviderCapabilities Capabilities { get; }

    Task<ProviderModelCatalog> ListModelsAsync(CancellationToken cancellationToken);

    IAsyncEnumerable<ProviderModelInstallationEvent> InstallModelAsync(
        ModelId modelId,
        CancellationToken cancellationToken);

    Task<OperationResult> RemoveModelAsync(ModelId modelId, CancellationToken cancellationToken);

    IAsyncEnumerable<ProviderGenerationEvent> GenerateAsync(
        ProviderGenerationRequest request,
        CancellationToken cancellationToken);
}
