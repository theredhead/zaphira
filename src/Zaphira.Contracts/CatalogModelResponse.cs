namespace Zaphira.Contracts;

public sealed record CatalogModelResponse
{
    public CatalogModelResponse(
        string id,
        string displayName,
        IReadOnlyList<string> tags,
        IReadOnlyList<string> purposes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(tags);
        ArgumentNullException.ThrowIfNull(purposes);

        string[] materializedTags = tags.ToArray();
        if (materializedTags.Any(tag => tag is null))
        {
            throw new ArgumentException("Catalog model tags cannot contain null values.", nameof(tags));
        }

        string[] materializedPurposes = purposes.ToArray();
        if (materializedPurposes.Any(purpose => purpose is null))
        {
            throw new ArgumentException("Catalog model purposes cannot contain null values.", nameof(purposes));
        }

        Id = id;
        DisplayName = displayName;
        Tags = materializedTags;
        Purposes = materializedPurposes;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public IReadOnlyList<string> Tags { get; }

    public IReadOnlyList<string> Purposes { get; }
}
