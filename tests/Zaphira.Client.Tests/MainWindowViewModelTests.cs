using System.Runtime.CompilerServices;
using Zaphira.Client.Backend;
using Zaphira.Client.Chat;
using Zaphira.Client.Configuration;
using Zaphira.Client.ViewModels;
using Zaphira.Contracts;

namespace Zaphira.Client.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void ConstructorStartsInFirstRunWhenConfigurationRequestsIt()
    {
        MainWindowViewModel viewModel = new(ZaphiraClientConfiguration.Default());

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
        MainWindowViewModel viewModel = new(ZaphiraClientConfiguration.Default());

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
                models: [new ModelResponse("fake-chat", "Fake Chat", ["TextGeneration"])]));

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
            ZaphiraClientConfiguration.Default(),
            probe,
            new EmptyChatApiClient());

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
            new EmptyChatApiClient());

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
                ]));

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
                    ErrorResponse.ProviderUnavailable())));

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
            new EmptyChatApiClient());

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
}
