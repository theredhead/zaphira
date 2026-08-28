namespace Zaphira.Application.ModelCatalog;

public sealed record CatalogModelSummary
{
    public CatalogModelSummary(
        string id,
        string displayName,
        IReadOnlyList<string> tags,
        IReadOnlyList<CatalogModelPurpose> purposes)
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

        CatalogModelPurpose[] materializedPurposes = purposes.Distinct().ToArray();

        Id = id;
        DisplayName = displayName;
        Tags = materializedTags;
        Purposes = materializedPurposes;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public IReadOnlyList<string> Tags { get; }

    public IReadOnlyList<CatalogModelPurpose> Purposes { get; }
}
