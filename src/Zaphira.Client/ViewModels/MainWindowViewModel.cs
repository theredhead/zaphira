using CommunityToolkit.Mvvm.Input;
using Zaphira.Client.Chat;
using Zaphira.Client.Configuration;

namespace Zaphira.Client.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private ClientPage selectedPage;

    public MainWindowViewModel()
        : this(ZaphiraClientConfiguration.Default())
    {
    }

    public MainWindowViewModel(ZaphiraClientConfiguration configuration)
        : this(
            configuration,
            new HttpChatApiClient(new HttpClient
            {
                BaseAddress = configuration.BackendAddress
            }))
    {
    }

    public MainWindowViewModel(ZaphiraClientConfiguration configuration, IChatApiClient chatApiClient)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(chatApiClient);

        BackendAddressText = configuration.BackendAddress.ToString();
        BackendConnectionState = configuration.StartsInFirstRun
            ? BackendConnectionState.SetupRequired
            : BackendConnectionState.Connecting;
        selectedPage = configuration.StartsInFirstRun ? ClientPage.FirstRun : ClientPage.Chat;
        ChatWorkspace = new ChatWorkspaceViewModel(chatApiClient);
    }

    public string ApplicationTitle { get; } = "Zaphira";

    public string BackendAddressText { get; }

    public BackendConnectionState BackendConnectionState { get; private set; }

    public ChatWorkspaceViewModel ChatWorkspace { get; }

    public string BackendConnectionStateText => BackendConnectionState switch
    {
        BackendConnectionState.Connecting => "Connecting",
        BackendConnectionState.Connected => "Connected",
        BackendConnectionState.Unavailable => "Unavailable",
        BackendConnectionState.SetupRequired => "Setup required",
        _ => "Unknown"
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
}
