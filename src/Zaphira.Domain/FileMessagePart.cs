namespace Zaphira.Domain;

public sealed record FileMessagePart : IMessagePart
{
    public FileMessagePart(FileReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);

        Reference = reference;
    }

    public FileReference Reference { get; }
}
