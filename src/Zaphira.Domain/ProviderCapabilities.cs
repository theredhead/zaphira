namespace Zaphira.Domain;

public sealed record ProviderCapabilities
{
    public ProviderCapabilities(IEnumerable<ProviderCapability> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        Values = values.Distinct().ToArray();
    }

    public IReadOnlyList<ProviderCapability> Values { get; }

    public bool Contains(ProviderCapability capability) => Values.Contains(capability);
}
