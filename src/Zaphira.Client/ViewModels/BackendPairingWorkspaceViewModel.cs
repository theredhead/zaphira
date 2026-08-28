using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Zaphira.Client.Chat;
using Zaphira.Client.Configuration;
using Zaphira.Client.Pairing;
using Zaphira.Client.Security;
using Zaphira.Contracts;

namespace Zaphira.Client.ViewModels;

public partial class BackendPairingWorkspaceViewModel : ViewModelBase
{
    private readonly ZaphiraClientConfigurationLoader configurationLoader;
    private readonly TrustedBackendConnectionStore connectionStore;
    private readonly IRemoteBackendPairingClientFactory pairingClientFactory;
    private string remoteBackendAddressText;
    private string pairingCode = string.Empty;
    private string clientName = Environment.MachineName;
    private string statusText = "Remote backend not checked.";
    private bool isBusy;

    public BackendPairingWorkspaceViewModel(
        ZaphiraClientConfiguration configuration,
        ZaphiraClientConfigurationLoader configurationLoader,
        TrustedBackendConnectionStore connectionStore,
        IRemoteBackendPairingClientFactory pairingClientFactory)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configurationLoader);
        ArgumentNullException.ThrowIfNull(connectionStore);
        ArgumentNullException.ThrowIfNull(pairingClientFactory);

        this.configurationLoader = configurationLoader;
        this.connectionStore = connectionStore;
        this.pairingClientFactory = pairingClientFactory;
        remoteBackendAddressText = configuration.BackendAddress.ToString();
    }

    public ObservableCollection<KnownBackendConnectionViewModel> KnownConnections { get; } = [];

    public string RemoteBackendAddressText
    {
        get => remoteBackendAddressText;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            SetProperty(ref remoteBackendAddressText, value);
        }
    }

    public string PairingCode
    {
        get => pairingCode;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            SetProperty(ref pairingCode, value);
        }
    }

    public string ClientName
    {
        get => clientName;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            SetProperty(ref clientName, value);
        }
    }

    public string StatusText
    {
        get => statusText;
        private set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            SetProperty(ref statusText, value);
        }
    }

    public bool IsBusy
    {
        get => isBusy;
        private set => SetProperty(ref isBusy, value);
    }

    [RelayCommand]
    public async Task LoadKnownConnectionsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<TrustedBackendConnection> connections = await connectionStore.LoadAsync(cancellationToken);

        KnownConnections.Clear();
        foreach (TrustedBackendConnection connection in connections)
        {
            KnownConnections.Add(new KnownBackendConnectionViewModel(connection));
        }

        StatusText = connections.Count == 0
            ? "No paired backends."
            : $"{connections.Count} paired backend(s).";
    }

    [RelayCommand]
    public async Task CheckRemoteBackendAsync(CancellationToken cancellationToken)
    {
        if (!TryCreateBackendAddress(out Uri backendAddress))
        {
            return;
        }

        IsBusy = true;
        try
        {
            bool isReachable = await pairingClientFactory
                .Create(backendAddress)
                .CheckBackendAsync(cancellationToken);

            StatusText = isReachable
                ? "Zaphira backend is reachable."
                : "No Zaphira backend responded at that address.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task PairRemoteBackendAsync(CancellationToken cancellationToken)
    {
        if (!TryCreateBackendAddress(out Uri backendAddress))
        {
            return;
        }

        string code = PairingCode.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            StatusText = "Pairing code is required.";
            return;
        }

        string trimmedClientName = ClientName.Trim();
        if (string.IsNullOrWhiteSpace(trimmedClientName))
        {
            StatusText = "Client name is required.";
            return;
        }

        IsBusy = true;
        try
        {
            IRemoteBackendPairingClient pairingClient = pairingClientFactory.Create(backendAddress);
            CreatePairingResponse response = await pairingClient.PairAsync(
                code,
                trimmedClientName,
                cancellationToken);
            TrustedBackendConnection connection = new(
                backendAddress,
                response.BackendCertificateThumbprint,
                response.BackendDescription,
                response.PairingId,
                response.AccessToken);

            await connectionStore.SaveAsync(connection, cancellationToken);
            await configurationLoader.SaveAsync(
                new ZaphiraClientConfiguration(backendAddress, startsInFirstRun: false),
                cancellationToken);
            await LoadKnownConnectionsAsync(cancellationToken);

            StatusText = "Remote backend paired and saved.";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusText = ToStatusText(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task RemoveKnownConnectionAsync(
        KnownBackendConnectionViewModel connection,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);

        IsBusy = true;
        try
        {
            if (connection.PairingId != TrustedBackendConnection.NoPairingId)
            {
                await pairingClientFactory
                    .Create(connection.ToBackendAddress())
                    .RevokePairingAsync(connection.PairingId, connection.AccessToken, cancellationToken);
            }

            await connectionStore.RemoveAsync(connection.ToBackendAddress(), cancellationToken);
            KnownConnections.Remove(connection);
            StatusText = "Pairing removed.";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusText = ToStatusText(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool TryCreateBackendAddress(out Uri backendAddress)
    {
        string addressText = RemoteBackendAddressText.Trim();
        if (!Uri.TryCreate(addressText, UriKind.Absolute, out Uri? parsedAddress))
        {
            backendAddress = ZaphiraClientConfiguration.Default().BackendAddress;
            StatusText = "Backend address must be absolute.";
            return false;
        }

        backendAddress = parsedAddress;
        return true;
    }

    private static string ToStatusText(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception is ChatApiException chatApiException
            ? $"{chatApiException.Error.Message} {chatApiException.Error.Suggestion}"
            : exception.Message;
    }
}
