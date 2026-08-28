using Zaphira.Client.Configuration;
using Zaphira.Client.Pairing;
using Zaphira.Client.Security;
using Zaphira.Client.Storage;
using Zaphira.Client.ViewModels;
using Zaphira.Contracts;

namespace Zaphira.Client.Tests;

public sealed class BackendPairingWorkspaceViewModelTests
{
    [Fact]
    public async Task CheckRemoteBackendShowsReachableStatus()
    {
        TestContext context = CreateContext();
        BackendPairingWorkspaceViewModel viewModel = context.CreateViewModel();

        await viewModel.CheckRemoteBackendAsync(CancellationToken.None);

        Assert.Equal("Zaphira backend is reachable.", viewModel.StatusText);
        Assert.Equal(1, context.PairingClient.CheckBackendCallCount);

        context.DeleteHomeDirectory();
    }

    [Fact]
    public async Task PairRemoteBackendPersistsConfigurationAndTrustedConnection()
    {
        TestContext context = CreateContext();
        BackendPairingWorkspaceViewModel viewModel = context.CreateViewModel();
        viewModel.RemoteBackendAddressText = "https://backend.example";
        viewModel.PairingCode = "1234";
        viewModel.ClientName = "Test Client";

        await viewModel.PairRemoteBackendAsync(CancellationToken.None);

        ZaphiraClientConfiguration loadedConfiguration =
            await context.ConfigurationLoader.LoadOrCreateAsync(CancellationToken.None);
        IReadOnlyList<TrustedBackendConnection> connections =
            await context.ConnectionStore.LoadAsync(CancellationToken.None);

        Assert.Equal(new Uri("https://backend.example"), loadedConfiguration.BackendAddress);
        Assert.False(loadedConfiguration.StartsInFirstRun);
        TrustedBackendConnection connection = Assert.Single(connections);
        Assert.Equal(context.PairingResponse.PairingId, connection.PairingId);
        Assert.Equal(context.PairingResponse.AccessToken, connection.AccessToken);
        Assert.Equal(context.PairingResponse.BackendCertificateThumbprint, connection.CertificateThumbprint);
        Assert.Single(viewModel.KnownConnections);
        Assert.Equal("Remote backend paired and saved.", viewModel.StatusText);

        context.DeleteHomeDirectory();
    }

    [Fact]
    public async Task CreatePairingCodeStoresCodeInInput()
    {
        TestContext context = CreateContext();
        BackendPairingWorkspaceViewModel viewModel = context.CreateViewModel();
        viewModel.RemoteBackendAddressText = "https://backend.example";

        await viewModel.CreatePairingCodeAsync(CancellationToken.None);

        Assert.Equal("1234", viewModel.PairingCode);
        Assert.Contains("Pairing code 1234", viewModel.StatusText, StringComparison.Ordinal);

        context.DeleteHomeDirectory();
    }

    [Fact]
    public async Task RemoveKnownConnectionRevokesAndDeletesLocalConnection()
    {
        TestContext context = CreateContext();
        TrustedBackendConnection connection = new(
            new Uri("https://backend.example"),
            "ABCDEF123456",
            "Remote backend",
            context.PairingResponse.PairingId,
            context.PairingResponse.AccessToken);
        await context.ConnectionStore.SaveAsync(connection, CancellationToken.None);
        BackendPairingWorkspaceViewModel viewModel = context.CreateViewModel();
        await viewModel.LoadKnownConnectionsAsync(CancellationToken.None);
        KnownBackendConnectionViewModel knownConnection = Assert.Single(viewModel.KnownConnections);

        await viewModel.RemoveKnownConnectionAsync(knownConnection, CancellationToken.None);

        IReadOnlyList<TrustedBackendConnection> connections =
            await context.ConnectionStore.LoadAsync(CancellationToken.None);

        Assert.Equal(1, context.PairingClient.RevokePairingCallCount);
        Assert.Empty(connections);
        Assert.Empty(viewModel.KnownConnections);
        Assert.Equal("Pairing removed.", viewModel.StatusText);

        context.DeleteHomeDirectory();
    }

    private static TestContext CreateContext()
    {
        string homeDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        ZaphiraClientDataDirectories dataDirectories = ZaphiraClientDataDirectories.ForHomeDirectory(homeDirectory);
        ZaphiraClientConfigurationLoader configurationLoader = new(dataDirectories);
        TrustedBackendConnectionStore connectionStore = new(dataDirectories);
        CreatePairingResponse pairingResponse = new(
            Guid.NewGuid(),
            "pairing-token",
            "ABCDEF123456",
            "Remote backend");
        FakeRemoteBackendPairingClient pairingClient = new(pairingResponse);

        return new TestContext(
            homeDirectory,
            configurationLoader,
            connectionStore,
            pairingClient,
            pairingResponse);
    }

    private sealed record TestContext(
        string HomeDirectory,
        ZaphiraClientConfigurationLoader ConfigurationLoader,
        TrustedBackendConnectionStore ConnectionStore,
        FakeRemoteBackendPairingClient PairingClient,
        CreatePairingResponse PairingResponse)
    {
        public BackendPairingWorkspaceViewModel CreateViewModel() =>
            new(
                new ZaphiraClientConfiguration(new Uri("https://localhost:5051"), startsInFirstRun: true),
                ConfigurationLoader,
                ConnectionStore,
                new FakeRemoteBackendPairingClientFactory(PairingClient));

        public void DeleteHomeDirectory()
        {
            if (Directory.Exists(HomeDirectory))
            {
                Directory.Delete(HomeDirectory, recursive: true);
            }
        }
    }

    private sealed class FakeRemoteBackendPairingClientFactory(FakeRemoteBackendPairingClient pairingClient)
        : IRemoteBackendPairingClientFactory
    {
        public IRemoteBackendPairingClient Create(Uri backendAddress)
        {
            ArgumentNullException.ThrowIfNull(backendAddress);

            return pairingClient;
        }
    }

    private sealed class FakeRemoteBackendPairingClient(CreatePairingResponse pairingResponse)
        : IRemoteBackendPairingClient
    {
        public int CheckBackendCallCount { get; private set; }

        public int PairCallCount { get; private set; }

        public int RevokePairingCallCount { get; private set; }

        public Task<bool> CheckBackendAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CheckBackendCallCount++;

            return Task.FromResult(true);
        }

        public Task<CreatePairingCodeResponse> CreatePairingCodeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(new CreatePairingCodeResponse("1234", DateTimeOffset.UtcNow.AddMinutes(10)));
        }

        public Task<CreatePairingResponse> PairAsync(
            string pairingCode,
            string clientName,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pairingCode);
            ArgumentException.ThrowIfNullOrWhiteSpace(clientName);
            cancellationToken.ThrowIfCancellationRequested();
            PairCallCount++;

            return Task.FromResult(pairingResponse);
        }

        public Task RevokePairingAsync(Guid pairingId, string accessToken, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RevokePairingCallCount++;

            return Task.CompletedTask;
        }
    }
}
