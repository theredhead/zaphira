namespace Zaphira.Application.ModelCatalog;

public interface ICatalogSource
{
    string Id { get; }

    string DisplayName { get; }

    Task<CatalogSourceResult> LoadAsync(CancellationToken cancellationToken);
}
