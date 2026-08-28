namespace Zaphira.Contracts;

public sealed record ModelCatalogResponse
{
    public ModelCatalogResponse(
        bool isFromCache,
        string message,
        string suggestion,
        IReadOnlyList<CatalogModelResponse> models)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(suggestion);
        ArgumentNullException.ThrowIfNull(models);

        CatalogModelResponse[] materializedModels = models.ToArray();
        if (materializedModels.Any(model => model is null))
        {
            throw new ArgumentException("Catalog response models cannot contain null values.", nameof(models));
        }

        IsFromCache = isFromCache;
        Message = message;
        Suggestion = suggestion;
        Models = materializedModels;
    }

    public bool IsFromCache { get; }

    public string Message { get; }

    public string Suggestion { get; }

    public IReadOnlyList<CatalogModelResponse> Models { get; }
}
