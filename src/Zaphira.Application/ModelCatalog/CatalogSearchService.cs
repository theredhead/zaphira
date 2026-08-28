namespace Zaphira.Application.ModelCatalog;

public sealed class CatalogSearchService
{
    public IReadOnlyList<CatalogModelSearchResult> Search(
        IReadOnlyList<CatalogModelSummary> models,
        CatalogSearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(models);
        ArgumentNullException.ThrowIfNull(request);

        CatalogModelSummary[] materializedModels = models.ToArray();
        if (materializedModels.Any(model => model is null))
        {
            throw new ArgumentException("Catalog search models cannot contain null values.", nameof(models));
        }

        return materializedModels
            .Where(model => MatchesQuery(model, request.Query))
            .Where(model => MatchesPurpose(model, request.Purposes))
            .Select(model => new CatalogModelSearchResult(
                model,
                DetermineCompatibilityStatus(model),
                DetermineCompatibilityConfidence(model),
                ExplainMatch(model, request)))
            .ToArray();
    }

    private static bool MatchesQuery(CatalogModelSummary model, string query) =>
        string.IsNullOrWhiteSpace(query)
        || model.Id.Contains(query, StringComparison.OrdinalIgnoreCase)
        || model.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesPurpose(CatalogModelSummary model, IReadOnlyList<CatalogModelPurpose> purposes) =>
        purposes.Count == 0 || purposes.Any(model.Purposes.Contains);

    private static CatalogCompatibilityStatus DetermineCompatibilityStatus(CatalogModelSummary model)
    {
        if (model.Purposes.Contains(CatalogModelPurpose.Embeddings))
        {
            return CatalogCompatibilityStatus.Unsupported;
        }

        if (model.Purposes.Contains(CatalogModelPurpose.GeneralChat)
            || model.Purposes.Contains(CatalogModelPurpose.Coding))
        {
            return CatalogCompatibilityStatus.DirectlyUsable;
        }

        if (model.Purposes.Contains(CatalogModelPurpose.Vision))
        {
            return CatalogCompatibilityStatus.PossiblyUsable;
        }

        return CatalogCompatibilityStatus.Unknown;
    }

    private static CatalogCompatibilityConfidence DetermineCompatibilityConfidence(CatalogModelSummary model)
    {
        CatalogCompatibilityStatus status = DetermineCompatibilityStatus(model);

        return status switch
        {
            CatalogCompatibilityStatus.DirectlyUsable => CatalogCompatibilityConfidence.Medium,
            CatalogCompatibilityStatus.PossiblyUsable => CatalogCompatibilityConfidence.Low,
            CatalogCompatibilityStatus.Unsupported => CatalogCompatibilityConfidence.High,
            _ => CatalogCompatibilityConfidence.Low
        };
    }

    private static string ExplainMatch(CatalogModelSummary model, CatalogSearchRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Query)
            && (model.Id.Contains(request.Query, StringComparison.OrdinalIgnoreCase)
                || model.DisplayName.Contains(request.Query, StringComparison.OrdinalIgnoreCase)))
        {
            return $"Matched name or id: {request.Query}.";
        }

        foreach (CatalogModelPurpose purpose in request.Purposes)
        {
            if (model.Purposes.Contains(purpose))
            {
                return $"Matched purpose: {purpose}.";
            }
        }

        return "Matched catalog filters.";
    }
}
