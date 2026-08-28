using System.Text.Json.Serialization;

namespace Zaphira.Contracts;

public sealed record ModelListResponse
{
    public ModelListResponse(string providerId, string providerDisplayName, IReadOnlyList<ModelResponse> models)
        : this(providerId, providerDisplayName, models, GetDefaultActiveModelId(models), HasDefaultActiveModel(models))
    {
    }

    [JsonConstructor]
    public ModelListResponse(
        string providerId,
        string providerDisplayName,
        IReadOnlyList<ModelResponse> models,
        string activeModelId,
        bool hasActiveModel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerDisplayName);
        ArgumentNullException.ThrowIfNull(models);
        ArgumentException.ThrowIfNullOrWhiteSpace(activeModelId);

        ModelResponse[] materializedModels = models.ToArray();
        if (materializedModels.Any(model => model is null))
        {
            throw new ArgumentException("Models cannot contain null values.", nameof(models));
        }

        ProviderId = providerId;
        ProviderDisplayName = providerDisplayName;
        Models = materializedModels;
        ActiveModelId = activeModelId;
        HasActiveModel = hasActiveModel;
    }

    public string ProviderId { get; }

    public string ProviderDisplayName { get; }

    public IReadOnlyList<ModelResponse> Models { get; }

    public string ActiveModelId { get; }

    public bool HasActiveModel { get; }

    private static string GetDefaultActiveModelId(IReadOnlyList<ModelResponse> models)
    {
        ArgumentNullException.ThrowIfNull(models);

        return models.Count > 0
            ? models[0].Id
            : "__zaphira_no_active_model__";
    }

    private static bool HasDefaultActiveModel(IReadOnlyList<ModelResponse> models)
    {
        ArgumentNullException.ThrowIfNull(models);

        return models.Count > 0;
    }
}
