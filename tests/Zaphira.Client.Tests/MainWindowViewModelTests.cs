using System.Runtime.CompilerServices;
using Zaphira.Client.Backend;
using Zaphira.Client.Chat;
using Zaphira.Client.ModelCatalog;
using Zaphira.Client.Configuration;
using Zaphira.Client.Pairing;
using Zaphira.Client.Security;
using Zaphira.Client.Storage;
using Zaphira.Client.ViewModels;
using Zaphira.Contracts;

namespace Zaphira.Client.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void ConstructorStartsInFirstRunWhenConfigurationRequestsIt()
    {
        MainWindowViewModel viewModel = new(
            new ZaphiraClientConfiguration(new Uri("https://localhost:5051"), startsInFirstRun: true));

        Assert.Equal(ClientPage.FirstRun, viewModel.SelectedPage);
        Assert.Equal(BackendConnectionState.SetupRequired, viewModel.BackendConnectionState);
    }

    [Fact]
    public void NavigationCommandsChangeSelectedPage()
    {
        MainWindowViewModel viewModel = new(ZaphiraClientConfiguration.Default());

        viewModel.ShowSettingsCommand.Execute(null);
        Assert.Equal(ClientPage.Settings, viewModel.SelectedPage);

        viewModel.ShowChatCommand.Execute(null);
        Assert.Equal(ClientPage.Chat, viewModel.SelectedPage);
    }

    [Fact]
    public void ReturnFromSettingsRestoresSetupPage()
    {
        MainWindowViewModel viewModel = new(
            new ZaphiraClientConfiguration(new Uri("https://localhost:5051"), startsInFirstRun: true));

        viewModel.ShowSettingsCommand.Execute(null);
        viewModel.ReturnFromSettingsCommand.Execute(null);

        Assert.Equal(ClientPage.FirstRun, viewModel.SelectedPage);
        Assert.False(viewModel.IsSettingsPageSelected);
    }

    [Fact]
    public void ReturnFromSettingsRestoresChatPage()
    {
        MainWindowViewModel viewModel = new(
            new ZaphiraClientConfiguration(new Uri("https://localhost:5051"), startsInFirstRun: false),
            new FakeBackendConnectionProbe(BackendConnectionProbeResult.Connected),
            new EmptyChatApiClient(
                conversations: [],
                models: [new ModelResponse("fake-chat", "Fake Chat", ["TextGeneration"])]),
            new EmptyModelCatalogApiClient(),
            CreateBackendPairingWorkspace());

        viewModel.ShowSettingsCommand.Execute(null);
        Assert.True(viewModel.IsSettingsPageSelected);

        viewModel.ReturnFromSettingsCommand.Execute(null);

        Assert.Equal(ClientPage.Chat, viewModel.SelectedPage);
        Assert.False(viewModel.IsSettingsPageSelected);
    }

    [Fact]
    public async Task InitializeSkipsProbeWhenSetupIsRequired()
    {
        FakeBackendConnectionProbe probe = new(BackendConnectionProbeResult.Connected);
        MainWindowViewModel viewModel = new(
            new ZaphiraClientConfiguration(new Uri("https://localhost:5051"), startsInFirstRun: true),
            probe,
            new EmptyChatApiClient(),
            new EmptyModelCatalogApiClient(),
            CreateBackendPairingWorkspace());

        await viewModel.InitializeAsync(CancellationToken.None);

        Assert.Equal(0, probe.CheckConnectionCallCount);
        Assert.Equal(BackendConnectionState.SetupRequired, viewModel.BackendConnectionState);
    }

    [Fact]
    public async Task InitializeMarksBackendUnavailableWhenProbeFails()
    {
        MainWindowViewModel viewModel = new(
            new ZaphiraClientConfiguration(new Uri("https://localhost:5051"), startsInFirstRun: false),
            new FakeBackendConnectionProbe(BackendConnectionProbeResult.Unavailable),
            new EmptyChatApiClient(),
            new EmptyModelCatalogApiClient(),
            CreateBackendPairingWorkspace());

        await viewModel.InitializeAsync(CancellationToken.None);

        Assert.Equal(BackendConnectionState.Unavailable, viewModel.BackendConnectionState);
        Assert.Equal("Unavailable", viewModel.BackendConnectionStateText);
        Assert.Equal(
            "Start the backend or check the backend address, then try again.",
            viewModel.AvailabilitySuggestionText);
        Assert.True(viewModel.IsBackendUnavailable);
    }

    [Fact]
    public async Task InitializeLoadsConversationsWhenBackendIsConnected()
    {
        Guid conversationId = Guid.NewGuid();
        MainWindowViewModel viewModel = new(
            new ZaphiraClientConfiguration(new Uri("https://localhost:5051"), startsInFirstRun: false),
            new FakeBackendConnectionProbe(BackendConnectionProbeResult.Connected),
            new EmptyChatApiClient(
                conversations:
                [
                    new ConversationResponse(
                        conversationId,
                        "Research",
                        "Ready",
                        0,
                        DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow)
                ],
                models:
                [
                    new ModelResponse("fake-chat", "Fake Chat", ["TextGeneration"])
                ]),
            new EmptyModelCatalogApiClient(),
            CreateBackendPairingWorkspace());

        await viewModel.InitializeAsync(CancellationToken.None);

        Assert.Equal(BackendConnectionState.Connected, viewModel.BackendConnectionState);
        Assert.Single(viewModel.ChatWorkspace.Conversations);
        Assert.Equal(conversationId, viewModel.ChatWorkspace.SelectedConversation.Id);
    }

    [Fact]
    public async Task InitializeMarksProviderUnavailableWhenModelListFails()
    {
        MainWindowViewModel viewModel = new(
            new ZaphiraClientConfiguration(new Uri("https://localhost:5051"), startsInFirstRun: false),
            new FakeBackendConnectionProbe(BackendConnectionProbeResult.Connected),
            new EmptyChatApiClient(
                conversations: [],
                models: [],
                modelListFailure: new ChatApiException(
                    503,
                    ErrorResponse.ProviderUnavailable())),
            new EmptyModelCatalogApiClient(),
            CreateBackendPairingWorkspace());

        await viewModel.InitializeAsync(CancellationToken.None);

        Assert.Equal(BackendConnectionState.ProviderUnavailable, viewModel.BackendConnectionState);
        Assert.Equal("Provider unavailable", viewModel.BackendConnectionStateText);
        Assert.Equal(
            "Start the provider, go online if needed, then try again.",
            viewModel.AvailabilitySuggestionText);
        Assert.True(viewModel.HasBlockingAvailabilityState);
    }

    [Fact]
    public async Task InitializeMarksNoInstalledModelWhenCatalogIsEmpty()
    {
        MainWindowViewModel viewModel = new(
            new ZaphiraClientConfiguration(new Uri("https://localhost:5051"), startsInFirstRun: false),
            new FakeBackendConnectionProbe(BackendConnectionProbeResult.Connected),
            new EmptyChatApiClient(),
            new EmptyModelCatalogApiClient(),
            CreateBackendPairingWorkspace());

        await viewModel.InitializeAsync(CancellationToken.None);

        Assert.Equal(BackendConnectionState.NoInstalledModel, viewModel.BackendConnectionState);
        Assert.Equal("No installed model", viewModel.BackendConnectionStateText);
        Assert.Equal(
            "Install a local model or choose settings to configure a provider.",
            viewModel.AvailabilitySuggestionText);
        Assert.True(viewModel.HasBlockingAvailabilityState);
    }

    private sealed class FakeBackendConnectionProbe : IBackendConnectionProbe
    {
        private readonly BackendConnectionProbeResult result;

        public FakeBackendConnectionProbe(BackendConnectionProbeResult result)
        {
            this.result = result;
        }

        public int CheckConnectionCallCount { get; private set; }

        public Task<BackendConnectionProbeResult> CheckConnectionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CheckConnectionCallCount++;

            return Task.FromResult(result);
        }
    }

    private static BackendPairingWorkspaceViewModel CreateBackendPairingWorkspace()
    {
        string homeDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        ZaphiraClientDataDirectories dataDirectories = ZaphiraClientDataDirectories.ForHomeDirectory(homeDirectory);

        return new BackendPairingWorkspaceViewModel(
            new ZaphiraClientConfiguration(new Uri("https://localhost:5051"), startsInFirstRun: false),
            new ZaphiraClientConfigurationLoader(dataDirectories),
            new TrustedBackendConnectionStore(dataDirectories),
            new FakeRemoteBackendPairingClientFactory());
    }

    private sealed class FakeRemoteBackendPairingClientFactory : IRemoteBackendPairingClientFactory
    {
        public IRemoteBackendPairingClient Create(Uri backendAddress)
        {
            ArgumentNullException.ThrowIfNull(backendAddress);

            return new FakeRemoteBackendPairingClient();
        }
    }

    private sealed class FakeRemoteBackendPairingClient : IRemoteBackendPairingClient
    {
        public Task<bool> CheckBackendAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

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
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(new CreatePairingResponse(
                Guid.NewGuid(),
                "token",
                "thumbprint",
                "Fake backend"));
        }

        public Task RevokePairingAsync(Guid pairingId, string accessToken, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }
    }

    private sealed class EmptyChatApiClient : IChatApiClient
    {
        private readonly IReadOnlyList<ConversationResponse> conversations;
        private readonly IReadOnlyList<ModelResponse> models;
        private readonly ChatApiException modelListFailure;

        public EmptyChatApiClient()
            : this([], [])
        {
        }

        public EmptyChatApiClient(
            IReadOnlyList<ConversationResponse> conversations,
            IReadOnlyList<ModelResponse> models,
            ChatApiException? modelListFailure = null)
        {
            ArgumentNullException.ThrowIfNull(conversations);
            ArgumentNullException.ThrowIfNull(models);

            this.conversations = conversations;
            this.models = models;
            this.modelListFailure = modelListFailure ?? ChatApiException.None;
        }

        public Task<ModelListResponse> GetInstalledModelsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (modelListFailure != ChatApiException.None)
            {
                throw modelListFailure;
            }

            return Task.FromResult(new ModelListResponse("fake", "Fake Provider", models));
        }

        public Task SelectActiveModelAsync(string modelId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("This test client does not select active models.");

        public async IAsyncEnumerable<ModelInstallationStreamResponse> InstallModelAsync(
            string modelId,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.CompletedTask;
            yield break;
        }

        public Task RemoveModelAsync(string modelId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("This test client does not remove models.");

        public Task<IReadOnlyList<ConversationResponse>> GetConversationsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(conversations);
        }

        public Task<ConversationResponse> CreateConversationAsync(string title, CancellationToken cancellationToken) =>
            throw new NotSupportedException("This test client does not create conversations.");

        public Task<ConversationResponse> RenameConversationAsync(
            Guid conversationId,
            string title,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("This test client does not rename conversations.");

        public Task DeleteConversationAsync(Guid conversationId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("This test client does not delete conversations.");

        public Task<IReadOnlyList<ChatMessageResponse>> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<IReadOnlyList<ChatMessageResponse>>([]);
        }

        public Task<SendMessageResponse> SendMessageAsync(
            Guid conversationId,
            string modelId,
            string text,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("This test client does not send messages.");

        public async IAsyncEnumerable<GenerationStreamResponse> StreamMessageAsync(
            Guid conversationId,
            Guid assistantMessageId,
            string modelId,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.CompletedTask;
            yield break;
        }

        public Task CancelMessageAsync(Guid conversationId, Guid assistantMessageId, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class EmptyModelCatalogApiClient : IModelCatalogApiClient
    {
        public Task<ModelCatalogResponse> GetCatalogAsync(
            string query,
            string purpose,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(new ModelCatalogResponse(
                isFromCache: false,
                "Catalog loaded.",
                "Search or filter the catalog.",
                []));
        }

        public Task<ModelCatalogResponse> SyncCatalogAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(new ModelCatalogResponse(
                isFromCache: false,
                "Catalog loaded.",
                "Search or filter the catalog.",
                []));
        }
    }
}
