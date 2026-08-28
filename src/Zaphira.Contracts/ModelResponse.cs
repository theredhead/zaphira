namespace Zaphira.Contracts;

public sealed record ModelResponse
{
    public ModelResponse(string id, string displayName, IReadOnlyList<string> capabilities)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(capabilities);

        string[] materializedCapabilities = capabilities.ToArray();
        if (materializedCapabilities.Any(capability => capability is null))
        {
            throw new ArgumentException("Model capabilities cannot contain null values.", nameof(capabilities));
        }

        Id = id;
        DisplayName = displayName;
        Capabilities = materializedCapabilities;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public IReadOnlyList<string> Capabilities { get; }
}
