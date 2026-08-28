namespace Zaphira.Contracts;

public sealed record InstallModelRequest
{
    public InstallModelRequest(string modelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);

        ModelId = modelId;
    }

    public string ModelId { get; }
}
