namespace Zaphira.Application.ModelCatalog;

public sealed record CatalogSourceResult
{
    private CatalogSourceResult(
        CatalogSourceResultStatus status,
        IReadOnlyList<CatalogModelSummary> models,
        string message,
        string suggestion)
    {
        ArgumentNullException.ThrowIfNull(models);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(suggestion);

        CatalogModelSummary[] materializedModels = models.ToArray();
        if (materializedModels.Any(model => model is null))
        {
            throw new ArgumentException("Catalog models cannot contain null values.", nameof(models));
        }

        Status = status;
        Models = materializedModels;
        Message = message;
        Suggestion = suggestion;
    }

    public CatalogSourceResultStatus Status { get; }

    public IReadOnlyList<CatalogModelSummary> Models { get; }

    public string Message { get; }

    public string Suggestion { get; }

    public bool IsAvailable => Status == CatalogSourceResultStatus.Available;

    public static CatalogSourceResult Available(IReadOnlyList<CatalogModelSummary> models) =>
        new(
            CatalogSourceResultStatus.Available,
            models,
            "Catalog loaded.",
            "Search or filter the catalog.");

    public static CatalogSourceResult Unavailable(string message, string suggestion) =>
        new(CatalogSourceResultStatus.Unavailable, [], message, suggestion);
}
