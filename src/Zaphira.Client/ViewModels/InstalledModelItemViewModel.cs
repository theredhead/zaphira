using Zaphira.Contracts;

namespace Zaphira.Client.ViewModels;

public sealed class InstalledModelItemViewModel : ViewModelBase
{
    private bool isActive;

    public InstalledModelItemViewModel(ModelResponse response, string activeModelId)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(activeModelId);

        Id = response.Id;
        DisplayName = response.DisplayName;
        CapabilitiesText = response.Capabilities.Count > 0
            ? string.Join(", ", response.Capabilities)
            : "No capabilities reported";
        isActive = response.Id == activeModelId;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string CapabilitiesText { get; }

    public bool IsActive
    {
        get => isActive;
        private set
        {
            if (SetProperty(ref isActive, value))
            {
                OnPropertyChanged(nameof(ActiveText));
            }
        }
    }

    public string ActiveText => IsActive ? "Active" : "Installed";

    public void MarkActive(bool value) => IsActive = value;
}
