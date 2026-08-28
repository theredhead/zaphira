using CommunityToolkit.Mvvm.Input;
using Zaphira.Client.Backend;
using Zaphira.Client.Chat;
using Zaphira.Client.Configuration;
using Zaphira.Contracts;

namespace Zaphira.Client.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private ClientPage selectedPage;
    private BackendConnectionState backendConnectionState;

    public MainWindowViewModel()
        : this(ZaphiraClientConfiguration.Default())
    {
    }

    public MainWindowViewModel(ZaphiraClientConfiguration configuration)
        : this(
            configuration,
            new HttpBackendConnectionProbe(new HttpClient
            {
                BaseAddress = configuration.BackendAddress
            }),
            new HttpChatApiClient(new HttpClient
            {
                BaseAddress = configuration.BackendAddress
            }))
    {
    }

    public MainWindowViewModel(
        ZaphiraClientConfiguration configuration,
        IBackendConnectionProbe backendConnectionProbe,
        IChatApiClient chatApiClient)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(backendConnectionProbe);
        ArgumentNullException.ThrowIfNull(chatApiClient);

        BackendConnectionProbe = backendConnectionProbe;
        BackendAddressText = configuration.BackendAddress.ToString();
        backendConnectionState = configuration.StartsInFirstRun
            ? BackendConnectionState.SetupRequired
            : BackendConnectionState.Connecting;
        selectedPage = configuration.StartsInFirstRun ? ClientPage.FirstRun : ClientPage.Chat;
        ChatWorkspace = new ChatWorkspaceViewModel(chatApiClient);
    }

    public string ApplicationTitle { get; } = "Zaphira";

    public string BackendAddressText { get; }

    public BackendConnectionState BackendConnectionState
    {
        get => backendConnectionState;
        private set
        {
            if (SetProperty(ref backendConnectionState, value))
            {
                OnPropertyChanged(nameof(BackendConnectionStateText));
                OnPropertyChanged(nameof(AvailabilitySuggestionText));
                OnPropertyChanged(nameof(IsBackendUnavailable));
                OnPropertyChanged(nameof(HasBlockingAvailabilityState));
            }
        }
    }

    public bool IsBackendUnavailable => BackendConnectionState == BackendConnectionState.Unavailable;

    public bool HasBlockingAvailabilityState => BackendConnectionState is BackendConnectionState.Unavailable
        or BackendConnectionState.ProviderUnavailable
        or BackendConnectionState.NoInstalledModel;

    public ChatWorkspaceViewModel ChatWorkspace { get; }

    private IBackendConnectionProbe BackendConnectionProbe { get; }

    public string BackendConnectionStateText => BackendConnectionState switch
    {
        BackendConnectionState.Connecting => "Connecting",
        BackendConnectionState.Connected => "Connected",
        BackendConnectionState.Unavailable => "Unavailable",
        BackendConnectionState.SetupRequired => "Setup required",
        BackendConnectionState.ProviderUnavailable => "Provider unavailable",
        BackendConnectionState.NoInstalledModel => "No installed model",
        _ => "Unknown"
    };

    public string AvailabilitySuggestionText => BackendConnectionState switch
    {
        BackendConnectionState.Unavailable => "Start the backend or check the backend address, then try again.",
        BackendConnectionState.ProviderUnavailable => "Start the provider, go online if needed, then try again.",
        BackendConnectionState.NoInstalledModel => "Install a local model or choose settings to configure a provider.",
        BackendConnectionState.SetupRequired => "Complete setup to connect Zaphira.",
        _ => string.Empty
    };

    public ClientPage SelectedPage
    {
        get => selectedPage;
        private set
        {
            if (SetProperty(ref selectedPage, value))
            {
                OnPropertyChanged(nameof(SelectedPageTitle));
            }
        }
    }

    public string SelectedPageTitle => SelectedPage switch
    {
        ClientPage.FirstRun => "Setup",
        ClientPage.Chat => "Chat",
        ClientPage.Settings => "Settings",
        _ => "Zaphira"
    };

    [RelayCommand]
    private void ShowFirstRun() => SelectedPage = ClientPage.FirstRun;

    [RelayCommand]
    private void ShowChat() => SelectedPage = ClientPage.Chat;

    [RelayCommand]
    private void ShowSettings() => SelectedPage = ClientPage.Settings;

    [RelayCommand]
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (BackendConnectionState == BackendConnectionState.SetupRequired)
        {
            return;
        }

        await RefreshBackendConnectionAsync(cancellationToken);
    }

    [RelayCommand]
    public async Task RefreshBackendConnectionAsync(CancellationToken cancellationToken)
    {
        BackendConnectionState = BackendConnectionState.Connecting;

        BackendConnectionProbeResult result = await BackendConnectionProbe.CheckConnectionAsync(cancellationToken);
        BackendConnectionState = result == BackendConnectionProbeResult.Connected
            ? BackendConnectionState.Connected
            : BackendConnectionState.Unavailable;

        if (BackendConnectionState != BackendConnectionState.Connected)
        {
            return;
        }

        ModelListResponse models;
        try
        {
            models = await ChatWorkspace.GetInstalledModelsAsync(cancellationToken);
        }
        catch (ChatApiException exception) when (exception.Error.Code == ErrorResponse.ProviderUnavailable().Code)
        {
            BackendConnectionState = BackendConnectionState.ProviderUnavailable;
            return;
        }

        if (models.Models.Count == 0)
        {
            BackendConnectionState = BackendConnectionState.NoInstalledModel;
            return;
        }

        await ChatWorkspace.LoadAsync(cancellationToken);
    }
}
