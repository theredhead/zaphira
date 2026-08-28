using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Zaphira.Client.Chat;
using Zaphira.Client.Configuration;
using Zaphira.Client.Logging;
using Zaphira.Client.Security;
using Zaphira.Client.Storage;
using Zaphira.Client.ViewModels;
using Zaphira.Client.Views;

namespace Zaphira.Client;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _ = InitializeDesktopAsync(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task InitializeDesktopAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        ZaphiraClientConfiguration configuration = await LoadConfigurationAsync();
        IChatApiClient chatApiClient = await CreateChatApiClientAsync(configuration);
        MainWindow mainWindow = new()
        {
            DataContext = new MainWindowViewModel(configuration, chatApiClient),
        };

        desktop.MainWindow = mainWindow;
        mainWindow.Show();
        mainWindow.Activate();
    }

    private static async Task<ZaphiraClientConfiguration> LoadConfigurationAsync()
    {
        ZaphiraClientDataDirectories dataDirectories = ZaphiraClientDataDirectories.ForCurrentUser();
        ZaphiraClientConfigurationLoader loader = new(dataDirectories);
        ZaphiraClientStartupLogger logger = new(dataDirectories);

        ZaphiraClientConfiguration configuration = await loader.LoadOrCreateAsync(CancellationToken.None);
        await logger.LogStartupAsync(configuration, CancellationToken.None);

        return configuration;
    }

    private static async Task<IChatApiClient> CreateChatApiClientAsync(ZaphiraClientConfiguration configuration)
    {
        ZaphiraClientDataDirectories dataDirectories = ZaphiraClientDataDirectories.ForCurrentUser();
        TrustedBackendConnectionStore connectionStore = new(dataDirectories);
        IReadOnlyList<TrustedBackendConnection> trustedConnections =
            await connectionStore.LoadAsync(CancellationToken.None);
        BackendCertificateTrust certificateTrust = new(trustedConnections);

        return new HttpChatApiClient(new HttpClient(certificateTrust.CreateHttpClientHandler(configuration.BackendAddress))
        {
            BaseAddress = configuration.BackendAddress
        });
    }
}
