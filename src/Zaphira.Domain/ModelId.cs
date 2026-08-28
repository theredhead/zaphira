namespace Zaphira.Domain;

public sealed record ModelId
{
    public static ModelId NoActiveModel { get; } = new("__zaphira_no_active_model__");

    public ModelId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
