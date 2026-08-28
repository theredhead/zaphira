namespace Zaphira.Application.ModelCatalog;

public interface IModelCatalogCache
{
    Task<CatalogCacheLookup> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(CatalogSnapshot snapshot, CancellationToken cancellationToken);
}
