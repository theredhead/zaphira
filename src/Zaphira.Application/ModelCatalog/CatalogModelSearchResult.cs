namespace Zaphira.Application.ModelCatalog;

public sealed record CatalogModelSearchResult
{
    public CatalogModelSearchResult(
        CatalogModelSummary model,
        CatalogCompatibilityStatus compatibilityStatus,
        CatalogCompatibilityConfidence compatibilityConfidence,
        string matchExplanation)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(matchExplanation);

        Model = model;
        CompatibilityStatus = compatibilityStatus;
        CompatibilityConfidence = compatibilityConfidence;
        MatchExplanation = matchExplanation;
    }

    public CatalogModelSummary Model { get; }

    public CatalogCompatibilityStatus CompatibilityStatus { get; }

    public CatalogCompatibilityConfidence CompatibilityConfidence { get; }

    public string MatchExplanation { get; }
}
