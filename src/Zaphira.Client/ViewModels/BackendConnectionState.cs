namespace Zaphira.Client.ViewModels;

public enum BackendConnectionState
{
    Connecting = 0,
    Connected = 1,
    Unavailable = 2,
    SetupRequired = 3,
    ProviderUnavailable = 4,
    NoInstalledModel = 5
}
