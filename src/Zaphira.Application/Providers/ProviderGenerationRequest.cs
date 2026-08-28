using Zaphira.Domain;

namespace Zaphira.Application.Providers;

public sealed record ProviderGenerationRequest
{
    public ProviderGenerationRequest(ModelId modelId, IEnumerable<ChatMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(modelId);
        ArgumentNullException.ThrowIfNull(messages);

        ChatMessage[] materializedMessages = messages.ToArray();
        if (materializedMessages.Length == 0)
        {
            throw new ArgumentException("A generation request must contain at least one message.", nameof(messages));
        }

        if (materializedMessages.Any(message => message is null))
        {
            throw new ArgumentException("Generation request messages cannot contain null values.", nameof(messages));
        }

        ModelId = modelId;
        Messages = materializedMessages;
    }

    public ModelId ModelId { get; }

    public IReadOnlyList<ChatMessage> Messages { get; }
}
