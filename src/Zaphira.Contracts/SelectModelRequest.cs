namespace Zaphira.Contracts;

public sealed record SelectModelRequest
{
    public SelectModelRequest(string modelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);

        ModelId = modelId;
    }

    public string ModelId { get; }
}
