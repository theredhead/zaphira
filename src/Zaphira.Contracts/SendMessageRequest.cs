namespace Zaphira.Contracts;

public sealed record SendMessageRequest
{
    public SendMessageRequest(string modelId, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        ModelId = modelId;
        Text = text;
    }

    public string ModelId { get; }

    public string Text { get; }
}
