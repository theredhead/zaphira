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
            ZaphiraClientConfiguration configuration = LoadConfiguration();
            IChatApiClient chatApiClient = CreateChatApiClient(configuration);
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(configuration, chatApiClient),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ZaphiraClientConfiguration LoadConfiguration()
    {
        ZaphiraClientDataDirectories dataDirectories = ZaphiraClientDataDirectories.ForCurrentUser();
        ZaphiraClientConfigurationLoader loader = new(dataDirectories);
        ZaphiraClientStartupLogger logger = new(dataDirectories);

        ZaphiraClientConfiguration configuration = loader.LoadOrCreateAsync(CancellationToken.None).GetAwaiter().GetResult();
        logger.LogStartupAsync(configuration, CancellationToken.None).GetAwaiter().GetResult();

        return configuration;
    }

    private static IChatApiClient CreateChatApiClient(ZaphiraClientConfiguration configuration)
    {
        ZaphiraClientDataDirectories dataDirectories = ZaphiraClientDataDirectories.ForCurrentUser();
        TrustedBackendConnectionStore connectionStore = new(dataDirectories);
        IReadOnlyList<TrustedBackendConnection> trustedConnections =
            connectionStore.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
        BackendCertificateTrust certificateTrust = new(trustedConnections);

        return new HttpChatApiClient(new HttpClient(certificateTrust.CreateHttpClientHandler(configuration.BackendAddress))
        {
            BaseAddress = configuration.BackendAddress
        });
    }
}
