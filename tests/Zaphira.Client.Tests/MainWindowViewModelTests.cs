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
                [
                    new ConversationResponse(
                        conversationId,
                        "Research",
                        "Ready",
                        0,
                        DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow)
                ]));

        await viewModel.InitializeAsync(CancellationToken.None);

        Assert.Equal(BackendConnectionState.Connected, viewModel.BackendConnectionState);
        Assert.Single(viewModel.ChatWorkspace.Conversations);
        Assert.Equal(conversationId, viewModel.ChatWorkspace.SelectedConversation.Id);
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

        public EmptyChatApiClient()
            : this([])
        {
        }

        public EmptyChatApiClient(IReadOnlyList<ConversationResponse> conversations)
        {
            ArgumentNullException.ThrowIfNull(conversations);

            this.conversations = conversations;
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
