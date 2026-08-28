namespace Zaphira.Domain;

public readonly record struct MessageId
{
    public MessageId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Message id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public static MessageId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
