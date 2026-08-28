namespace Zaphira.Contracts;

public sealed record ModelListResponse
{
    public ModelListResponse(string providerId, string providerDisplayName, IReadOnlyList<ModelResponse> models)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerDisplayName);
        ArgumentNullException.ThrowIfNull(models);

        ModelResponse[] materializedModels = models.ToArray();
        if (materializedModels.Any(model => model is null))
        {
            throw new ArgumentException("Models cannot contain null values.", nameof(models));
        }

        ProviderId = providerId;
        ProviderDisplayName = providerDisplayName;
        Models = materializedModels;
    }

    public string ProviderId { get; }

    public string ProviderDisplayName { get; }

    public IReadOnlyList<ModelResponse> Models { get; }
}
