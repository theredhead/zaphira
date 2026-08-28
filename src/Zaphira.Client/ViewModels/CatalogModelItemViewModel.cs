using Zaphira.Contracts;

namespace Zaphira.Client.ViewModels;

public sealed class CatalogModelItemViewModel
{
    public CatalogModelItemViewModel(CatalogModelResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        Id = response.Id;
        DisplayName = response.DisplayName;
        PurposesText = string.Join(", ", response.Purposes);
        CompatibilityText = $"{response.CompatibilityStatus} ({response.CompatibilityConfidence})";
        CompatibilityExplanation = response.CompatibilityExplanation;
        MatchExplanation = response.MatchExplanation;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string PurposesText { get; }

    public string CompatibilityText { get; }

    public string CompatibilityExplanation { get; }

    public string MatchExplanation { get; }
}
