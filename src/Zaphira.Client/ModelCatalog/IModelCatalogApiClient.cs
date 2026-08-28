using Zaphira.Contracts;

namespace Zaphira.Client.ModelCatalog;

public interface IModelCatalogApiClient
{
    Task<ModelCatalogResponse> GetCatalogAsync(
        string query,
        string purpose,
        CancellationToken cancellationToken);

    Task<ModelCatalogResponse> SyncCatalogAsync(CancellationToken cancellationToken);
}
