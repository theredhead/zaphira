namespace Zaphira.Application.ModelCatalog;

public sealed record ModelCatalogLoadResult
{
    private ModelCatalogLoadResult(
        CatalogSourceResultStatus status,
        IReadOnlyList<CatalogModelSummary> models,
        bool isFromCache,
        string message,
        string suggestion)
    {
        ArgumentNullException.ThrowIfNull(models);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(suggestion);

        CatalogModelSummary[] materializedModels = models.ToArray();
        if (materializedModels.Any(model => model is null))
        {
            throw new ArgumentException("Model catalog results cannot contain null values.", nameof(models));
        }

        Status = status;
        Models = materializedModels;
        IsFromCache = isFromCache;
        Message = message;
        Suggestion = suggestion;
    }

    public CatalogSourceResultStatus Status { get; }

    public IReadOnlyList<CatalogModelSummary> Models { get; }

    public bool IsFromCache { get; }

    public string Message { get; }

    public string Suggestion { get; }

    public bool IsAvailable => Status == CatalogSourceResultStatus.Available;

    public static ModelCatalogLoadResult Available(
        IReadOnlyList<CatalogModelSummary> models,
        bool isFromCache,
        string message,
        string suggestion) =>
        new(CatalogSourceResultStatus.Available, models, isFromCache, message, suggestion);

    public static ModelCatalogLoadResult Unavailable(string message, string suggestion) =>
        new(CatalogSourceResultStatus.Unavailable, [], isFromCache: false, message, suggestion);
}
