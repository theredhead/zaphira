namespace Zaphira.Contracts;

public sealed record CatalogModelResponse
{
    public CatalogModelResponse(
        string id,
        string displayName,
        IReadOnlyList<string> tags,
        IReadOnlyList<string> purposes,
        string compatibilityStatus,
        string compatibilityConfidence,
        string compatibilityExplanation,
        string matchExplanation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(tags);
        ArgumentNullException.ThrowIfNull(purposes);
        ArgumentException.ThrowIfNullOrWhiteSpace(compatibilityStatus);
        ArgumentException.ThrowIfNullOrWhiteSpace(compatibilityConfidence);
        ArgumentException.ThrowIfNullOrWhiteSpace(compatibilityExplanation);
        ArgumentException.ThrowIfNullOrWhiteSpace(matchExplanation);

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
        CompatibilityStatus = compatibilityStatus;
        CompatibilityConfidence = compatibilityConfidence;
        CompatibilityExplanation = compatibilityExplanation;
        MatchExplanation = matchExplanation;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public IReadOnlyList<string> Tags { get; }

    public IReadOnlyList<string> Purposes { get; }

    public string CompatibilityStatus { get; }

    public string CompatibilityConfidence { get; }

    public string CompatibilityExplanation { get; }

    public string MatchExplanation { get; }
}
