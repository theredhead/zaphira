namespace Zaphira.Application.ModelCatalog;

public sealed class ModelCatalogService
{
    private static readonly TimeSpan NormalCacheLifetime = TimeSpan.FromHours(24);

    private readonly ICatalogSource catalogSource;
    private readonly IModelCatalogCache modelCatalogCache;
    private readonly TimeProvider timeProvider;

    public ModelCatalogService(
        ICatalogSource catalogSource,
        IModelCatalogCache modelCatalogCache,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(catalogSource);
        ArgumentNullException.ThrowIfNull(modelCatalogCache);
        ArgumentNullException.ThrowIfNull(timeProvider);

        this.catalogSource = catalogSource;
        this.modelCatalogCache = modelCatalogCache;
        this.timeProvider = timeProvider;
    }

    public async Task<ModelCatalogLoadResult> LoadAsync(bool forceSync, CancellationToken cancellationToken)
    {
        CatalogCacheLookup cachedCatalog = await modelCatalogCache.LoadAsync(cancellationToken);
        if (!forceSync && cachedCatalog.Exists && IsFresh(cachedCatalog.Snapshot))
        {
            return ModelCatalogLoadResult.Available(
                cachedCatalog.Snapshot.Models,
                isFromCache: true,
                "Cached catalog loaded.",
                "Search or filter the cached catalog.");
        }

        CatalogSourceResult sourceResult = await catalogSource.LoadAsync(cancellationToken);
        if (sourceResult.IsAvailable)
        {
            CatalogSnapshot snapshot = new(
                catalogSource.Id,
                timeProvider.GetUtcNow(),
                sourceResult.Models);
            await modelCatalogCache.SaveAsync(snapshot, cancellationToken);

            return ModelCatalogLoadResult.Available(
                sourceResult.Models,
                isFromCache: false,
                sourceResult.Message,
                sourceResult.Suggestion);
        }

        if (cachedCatalog.Exists)
        {
            return ModelCatalogLoadResult.Available(
                cachedCatalog.Snapshot.Models,
                isFromCache: true,
                "Cached catalog loaded because sync is unavailable.",
                "Go online and sync again when possible.");
        }

        return ModelCatalogLoadResult.Unavailable(sourceResult.Message, sourceResult.Suggestion);
    }

    private bool IsFresh(CatalogSnapshot snapshot) =>
        timeProvider.GetUtcNow() - snapshot.FetchedAt <= NormalCacheLifetime;
}
