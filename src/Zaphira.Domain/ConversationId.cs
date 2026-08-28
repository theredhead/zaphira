namespace Zaphira.Domain;

public readonly record struct ConversationId
{
    public ConversationId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Conversation id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public static ConversationId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
