using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Net.Http.Headers;
using Zaphira.Client.Backend;
using Zaphira.Client.Chat;
using Zaphira.Client.Configuration;
using Zaphira.Client.Logging;
using Zaphira.Client.ModelCatalog;
using Zaphira.Client.Pairing;
using Zaphira.Client.Security;
using Zaphira.Client.Storage;
using Zaphira.Client.ViewModels;
using Zaphira.Client.Views;

namespace Zaphira.Client;

public partial class App : Application
{
    private LocalBackendProcessManager localBackendProcessManager = CreateMissingBackendProcessManager();

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

    private async Task InitializeDesktopAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        ZaphiraClientConfiguration configuration = await LoadConfigurationAsync();
        LocalBackendProcessManager localBackendProcessManager = CreateLocalBackendProcessManager(configuration);
        using HttpClient startupProbeHttpClient = CreateLocalBackendStartupProbeClient(configuration);
        LocalBackendStartupCoordinator startupCoordinator = new(
            configuration.BackendAddress,
            new HttpBackendConnectionProbe(startupProbeHttpClient),
            localBackendProcessManager,
            readinessCheckCount: 20,
            readinessCheckDelay: TimeSpan.FromMilliseconds(250));
        await startupCoordinator.EnsureLocalBackendIsAvailableAsync(CancellationToken.None);
        ZaphiraClientDataDirectories dataDirectories = ZaphiraClientDataDirectories.ForCurrentUser();
        ZaphiraClientConfigurationLoader configurationLoader = new(dataDirectories);
        TrustedBackendConnectionStore connectionStore = new(dataDirectories);
        HttpClient backendHttpClient = await CreateBackendHttpClientAsync(configuration);
        IBackendConnectionProbe backendConnectionProbe = new HttpBackendConnectionProbe(backendHttpClient);
        IChatApiClient chatApiClient = new HttpChatApiClient(backendHttpClient);
        IModelCatalogApiClient modelCatalogApiClient = new HttpModelCatalogApiClient(backendHttpClient);
        BackendPairingWorkspaceViewModel backendPairingWorkspace = new(
            configuration,
            configurationLoader,
            connectionStore,
            new HttpRemoteBackendPairingClientFactory());
        MainWindowViewModel viewModel = new(
            configuration,
            backendConnectionProbe,
            chatApiClient,
            modelCatalogApiClient,
            backendPairingWorkspace);
        MainWindow mainWindow = new()
        {
            DataContext = viewModel,
        };

        desktop.MainWindow = mainWindow;
        mainWindow.Show();
        mainWindow.Activate();
        this.localBackendProcessManager = localBackendProcessManager;
        desktop.ShutdownRequested += OnShutdownRequested;

        await viewModel.InitializeAsync(CancellationToken.None);
    }

    private async void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs eventArgs)
    {
        try
        {
            await localBackendProcessManager.StopAsync(CancellationToken.None);
        }
        catch (Exception)
        {
            // Shutdown should continue even if the owned backend has already exited.
        }
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

    private static async Task<HttpClient> CreateBackendHttpClientAsync(ZaphiraClientConfiguration configuration)
    {
        ZaphiraClientDataDirectories dataDirectories = ZaphiraClientDataDirectories.ForCurrentUser();
        TrustedBackendConnectionStore connectionStore = new(dataDirectories);
        IReadOnlyList<TrustedBackendConnection> trustedConnections =
            await connectionStore.LoadAsync(CancellationToken.None);
        BackendCertificateTrust certificateTrust = new(trustedConnections);

        HttpClient httpClient = new(certificateTrust.CreateHttpClientHandler(configuration.BackendAddress))
        {
            BaseAddress = configuration.BackendAddress
        };

        TrustedBackendConnection activeConnection = trustedConnections.FirstOrDefault(connection =>
            Uri.Compare(
                connection.BackendAddress,
                configuration.BackendAddress,
                UriComponents.HttpRequestUrl,
                UriFormat.SafeUnescaped,
                StringComparison.OrdinalIgnoreCase) == 0)
            ?? new TrustedBackendConnection(
                configuration.BackendAddress,
                "__zaphira_no_certificate_thumbprint__",
                "__zaphira_no_connection__");

        if (activeConnection.PairingId != TrustedBackendConnection.NoPairingId)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                activeConnection.AccessToken);
        }

        return httpClient;
    }

    private static HttpClient CreateLocalBackendStartupProbeClient(ZaphiraClientConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        })
        {
            BaseAddress = configuration.BackendAddress
        };
    }

    private static LocalBackendProcessManager CreateLocalBackendProcessManager(ZaphiraClientConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string serverOutputDirectory = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Zaphira.Server",
            "bin",
            "Debug",
            "net10.0"));
        LocalBackendPayloadLocation location = new LocalBackendPayloadLocator(serverOutputDirectory).Locate();
        string executablePath = location is AvailableLocalBackendPayload available
            ? available.ExecutablePath
            : Path.Combine(serverOutputDirectory, "Zaphira.Server.dll");
        string arguments = $"--Zaphira:Https:Port={configuration.BackendAddress.Port}";

        return new LocalBackendProcessManager(
            new OperatingSystemBackendProcessLauncher(),
            new LocalBackendProcessOptions(
                executablePath,
                arguments,
                serverOutputDirectory,
                startupRetryCount: 1,
                TimeSpan.FromMilliseconds(250)));
    }

    private static LocalBackendProcessManager CreateMissingBackendProcessManager() =>
        new(
            new OperatingSystemBackendProcessLauncher(),
            new LocalBackendProcessOptions(
                "__zaphira_missing_backend__",
                string.Empty,
                AppContext.BaseDirectory,
                startupRetryCount: 0,
                TimeSpan.Zero));

}
