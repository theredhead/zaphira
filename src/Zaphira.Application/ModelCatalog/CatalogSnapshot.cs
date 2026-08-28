namespace Zaphira.Application.ModelCatalog;

public sealed record CatalogSnapshot
{
    public CatalogSnapshot(string sourceId, DateTimeOffset fetchedAt, IReadOnlyList<CatalogModelSummary> models)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentNullException.ThrowIfNull(models);

        CatalogModelSummary[] materializedModels = models.ToArray();
        if (materializedModels.Any(model => model is null))
        {
            throw new ArgumentException("Catalog snapshot models cannot contain null values.", nameof(models));
        }

        SourceId = sourceId;
        FetchedAt = fetchedAt;
        Models = materializedModels;
    }

    public string SourceId { get; }

    public DateTimeOffset FetchedAt { get; }

    public IReadOnlyList<CatalogModelSummary> Models { get; }
}
