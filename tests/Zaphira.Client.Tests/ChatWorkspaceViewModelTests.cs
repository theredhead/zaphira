using System.Runtime.CompilerServices;
using Zaphira.Client.Chat;
using Zaphira.Client.ViewModels;
using Zaphira.Contracts;

namespace Zaphira.Client.Tests;

public sealed class ChatWorkspaceViewModelTests
{
    [Fact]
    public async Task LoadConversationsSelectsFirstConversationAndMessages()
    {
        Guid conversationId = Guid.NewGuid();
        Guid messageId = Guid.NewGuid();
        FakeChatApiClient chatApiClient = new(
            conversations:
            [
                new ConversationResponse(conversationId, "Research", "Hello", 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
            ],
            messages:
            [
                new ChatMessageResponse(
                    messageId,
                    conversationId,
                    "User",
                    "Completed",
                    [new MessagePartResponse("text", "Hello")],
                    DateTimeOffset.UtcNow)
            ]);
        ChatWorkspaceViewModel viewModel = new(chatApiClient);

        await viewModel.LoadAsync(CancellationToken.None);

        Assert.Single(viewModel.Conversations);
        Assert.Equal(conversationId, viewModel.SelectedConversation.Id);
        Assert.Single(viewModel.Messages);
        Assert.Equal("Hello", viewModel.Messages[0].DisplayText);
        Assert.Equal("Ready", viewModel.StatusText);
    }

    [Fact]
    public async Task SendMessageCreatesConversationWhenNeededAndStreamsAssistantMessage()
    {
        Guid conversationId = Guid.NewGuid();
        Guid userMessageId = Guid.NewGuid();
        Guid assistantMessageId = Guid.NewGuid();
        FakeChatApiClient chatApiClient = new(
            createdConversation: new ConversationResponse(
                conversationId,
                "New chat",
                "No messages yet.",
                0,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow),
            sendMessageResponse: new SendMessageResponse(userMessageId, assistantMessageId),
            streamResponses:
            [
                GenerationStreamResponse.TextDelta("Hello"),
                GenerationStreamResponse.TextDelta(" there"),
                GenerationStreamResponse.Completed()
            ]);
        ChatWorkspaceViewModel viewModel = new(chatApiClient)
        {
            ComposerText = "Say hello"
        };

        await viewModel.SendMessageAsync(CancellationToken.None);

        Assert.Equal(1, chatApiClient.CreateConversationCallCount);
        Assert.Equal(1, chatApiClient.SendMessageCallCount);
        Assert.Equal(2, viewModel.Messages.Count);
        Assert.Equal("User", viewModel.Messages[0].Role);
        Assert.Equal("Say hello", viewModel.Messages[0].DisplayText);
        Assert.Equal("Assistant", viewModel.Messages[1].Role);
        Assert.Equal("Hello there", viewModel.Messages[1].DisplayText);
        Assert.Equal("Completed", viewModel.Messages[1].Status);
        Assert.Equal(string.Empty, viewModel.ComposerText);
        Assert.False(viewModel.IsStreaming);
    }

    [Fact]
    public async Task SendMessageShowsApiErrorWithoutLeavingStreamingState()
    {
        FakeChatApiClient chatApiClient = new(
            failure: new ChatApiException(
                503,
                new ErrorResponse(
                    "provider_unavailable",
                    "The model provider is unavailable.",
                    "Start the provider and try again.")));
        ChatWorkspaceViewModel viewModel = new(chatApiClient)
        {
            ComposerText = "Hello"
        };

        await viewModel.SendMessageAsync(CancellationToken.None);

        Assert.False(viewModel.IsStreaming);
        Assert.Equal("The model provider is unavailable. Start the provider and try again.", viewModel.StatusText);
        Assert.Equal("Hello", viewModel.ComposerText);
    }

    [Fact]
    public async Task RenameConversationUpdatesSelectedConversationTitle()
    {
        Guid conversationId = Guid.NewGuid();
        FakeChatApiClient chatApiClient = new(
            conversations:
            [
                new ConversationResponse(conversationId, "Research", "Hello", 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
            ],
            renamedConversation: new ConversationResponse(
                conversationId,
                "Renamed",
                "Hello",
                1,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow));
        ChatWorkspaceViewModel viewModel = new(chatApiClient);

        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.SelectedConversationTitle = "Renamed";
        await viewModel.RenameConversationAsync(CancellationToken.None);

        Assert.Equal(1, chatApiClient.RenameConversationCallCount);
        Assert.Equal("Renamed", viewModel.SelectedConversation.Title);
        Assert.Equal("Renamed", viewModel.SelectedConversationTitle);
        Assert.Equal("Renamed", viewModel.StatusText);
    }

    [Fact]
    public async Task DeleteConversationRequiresConfirmationAndRemovesSelectedConversation()
    {
        Guid conversationId = Guid.NewGuid();
        FakeChatApiClient chatApiClient = new(
            conversations:
            [
                new ConversationResponse(conversationId, "Research", "Hello", 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
            ]);
        ChatWorkspaceViewModel viewModel = new(chatApiClient);

        await viewModel.LoadAsync(CancellationToken.None);
        viewModel.RequestDeleteConversationCommand.Execute(null);
        await viewModel.ConfirmDeleteConversationAsync(CancellationToken.None);

        Assert.Equal(1, chatApiClient.DeleteConversationCallCount);
        Assert.Empty(viewModel.Conversations);
        Assert.Empty(viewModel.Messages);
        Assert.False(viewModel.HasSelectedConversation);
        Assert.Equal("Deleted", viewModel.StatusText);
    }

    [Fact]
    public void MessageViewModelSplitsMarkdownCodeBlocks()
    {
        ChatMessageViewModel viewModel = ChatMessageViewModel.FromResponse(
            new ChatMessageResponse(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Assistant",
                "Completed",
                [new MessagePartResponse("text", "Before\n```csharp\nConsole.WriteLine(\"Hi\");\n```\nAfter")],
                DateTimeOffset.UtcNow));

        Assert.Equal(3, viewModel.RenderedParts.Count);
        Assert.False(viewModel.RenderedParts[0].IsCodeBlock);
        Assert.True(viewModel.RenderedParts[1].IsCodeBlock);
        Assert.Equal("csharp", viewModel.RenderedParts[1].Language);
        Assert.False(viewModel.RenderedParts[2].IsCodeBlock);
    }

    [Fact]
    public void MessageViewModelRendersMarkdownBlocks()
    {
        ChatMessageViewModel viewModel = ChatMessageViewModel.FromResponse(
            new ChatMessageResponse(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Assistant",
                "Completed",
                [
                    new MessagePartResponse(
                        "text",
                        "# Heading\n\nA paragraph that wraps\nacross lines.\n\n- First\n* Second\n\n> Useful quote")
                ],
                DateTimeOffset.UtcNow));

        Assert.Equal(5, viewModel.RenderedParts.Count);
        Assert.True(viewModel.RenderedParts[0].IsHeadingOne);
        Assert.Equal("Heading", viewModel.RenderedParts[0].Text);
        Assert.True(viewModel.RenderedParts[1].IsParagraph);
        Assert.Equal("A paragraph that wraps across lines.", viewModel.RenderedParts[1].Text);
        Assert.True(viewModel.RenderedParts[2].IsListItem);
        Assert.Equal("First", viewModel.RenderedParts[2].Text);
        Assert.True(viewModel.RenderedParts[3].IsListItem);
        Assert.Equal("Second", viewModel.RenderedParts[3].Text);
        Assert.True(viewModel.RenderedParts[4].IsQuote);
        Assert.Equal("Useful quote", viewModel.RenderedParts[4].Text);
    }

    [Fact]
    public void PendingAssistantHasNonNullEmptyParagraph()
    {
        ChatMessageViewModel viewModel = ChatMessageViewModel.PendingAssistant(Guid.NewGuid());

        Assert.Single(viewModel.RenderedParts);
        Assert.True(viewModel.RenderedParts[0].IsParagraph);
        Assert.Equal(string.Empty, viewModel.RenderedParts[0].Text);
        Assert.Equal(string.Empty, viewModel.RenderedParts[0].Language);
    }

    private sealed class FakeChatApiClient : IChatApiClient
    {
        private readonly IReadOnlyList<ConversationResponse> conversations;
        private readonly IReadOnlyList<ChatMessageResponse> messages;
        private readonly ConversationResponse createdConversation;
        private readonly ConversationResponse renamedConversation;
        private readonly SendMessageResponse sendMessageResponse;
        private readonly IReadOnlyList<GenerationStreamResponse> streamResponses;
        private readonly ChatApiException failure;

        public FakeChatApiClient(
            IReadOnlyList<ConversationResponse>? conversations = null,
            IReadOnlyList<ChatMessageResponse>? messages = null,
            ConversationResponse? createdConversation = null,
            ConversationResponse? renamedConversation = null,
            SendMessageResponse? sendMessageResponse = null,
            IReadOnlyList<GenerationStreamResponse>? streamResponses = null,
            ChatApiException? failure = null)
        {
            this.conversations = conversations ?? [];
            this.messages = messages ?? [];
            this.createdConversation = createdConversation
                ?? new ConversationResponse(Guid.NewGuid(), "New chat", "No messages yet.", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            this.renamedConversation = renamedConversation
                ?? new ConversationResponse(this.createdConversation.Id, "Renamed", "No messages yet.", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
            this.sendMessageResponse = sendMessageResponse
                ?? new SendMessageResponse(Guid.NewGuid(), Guid.NewGuid());
            this.streamResponses = streamResponses ?? [GenerationStreamResponse.Completed()];
            this.failure = failure ?? ChatApiException.None;
        }

        public int CreateConversationCallCount { get; private set; }

        public int RenameConversationCallCount { get; private set; }

        public int DeleteConversationCallCount { get; private set; }

        public int SendMessageCallCount { get; private set; }

        public Task<IReadOnlyList<ConversationResponse>> GetConversationsAsync(CancellationToken cancellationToken)
        {
            ThrowFailureIfNeeded();
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(conversations);
        }

        public Task<ConversationResponse> CreateConversationAsync(string title, CancellationToken cancellationToken)
        {
            ThrowFailureIfNeeded();
            cancellationToken.ThrowIfCancellationRequested();
            CreateConversationCallCount++;

            return Task.FromResult(createdConversation);
        }

        public Task<ConversationResponse> RenameConversationAsync(
            Guid conversationId,
            string title,
            CancellationToken cancellationToken)
        {
            ThrowFailureIfNeeded();
            cancellationToken.ThrowIfCancellationRequested();
            RenameConversationCallCount++;

            return Task.FromResult(renamedConversation);
        }

        public Task DeleteConversationAsync(Guid conversationId, CancellationToken cancellationToken)
        {
            ThrowFailureIfNeeded();
            cancellationToken.ThrowIfCancellationRequested();
            DeleteConversationCallCount++;

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ChatMessageResponse>> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken)
        {
            ThrowFailureIfNeeded();
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(messages);
        }

        public Task<SendMessageResponse> SendMessageAsync(
            Guid conversationId,
            string modelId,
            string text,
            CancellationToken cancellationToken)
        {
            ThrowFailureIfNeeded();
            cancellationToken.ThrowIfCancellationRequested();
            SendMessageCallCount++;

            return Task.FromResult(sendMessageResponse);
        }

        public async IAsyncEnumerable<GenerationStreamResponse> StreamMessageAsync(
            Guid conversationId,
            Guid assistantMessageId,
            string modelId,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ThrowFailureIfNeeded();
            foreach (GenerationStreamResponse response in streamResponses)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return response;
            }
        }

        public Task CancelMessageAsync(Guid conversationId, Guid assistantMessageId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }

        private void ThrowFailureIfNeeded()
        {
            if (failure != ChatApiException.None)
            {
                throw failure;
            }
        }
    }
}
