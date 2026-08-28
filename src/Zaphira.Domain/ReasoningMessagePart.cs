namespace Zaphira.Domain;

public sealed record ReasoningMessagePart : IMessagePart
{
    private const string HiddenSummary = "Reasoning not provided.";

    public ReasoningMessagePart(string summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        Summary = summary;
    }

    public string Summary { get; }

    public static ReasoningMessagePart Hidden() => new(HiddenSummary);
}
