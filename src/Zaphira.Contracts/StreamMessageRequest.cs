namespace Zaphira.Contracts;

public sealed record StreamMessageRequest
{
    public StreamMessageRequest(string modelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);

        ModelId = modelId;
    }

    public string ModelId { get; }
}
