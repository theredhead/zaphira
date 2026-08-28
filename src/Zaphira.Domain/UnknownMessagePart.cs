namespace Zaphira.Domain;

public sealed record UnknownMessagePart : IMessagePart
{
    public UnknownMessagePart(string originalKind, string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalKind);
        ArgumentNullException.ThrowIfNull(payload);

        OriginalKind = originalKind;
        Payload = payload;
    }

    public string OriginalKind { get; }

    public string Payload { get; }
}
