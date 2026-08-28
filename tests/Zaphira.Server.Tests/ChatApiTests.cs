using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Zaphira.Application.Providers;
using Zaphira.Contracts;
using Zaphira.Domain;

namespace Zaphira.Server.Tests;

public sealed class ChatApiTests
{
    [Fact]
    public async Task CreateConversationPersistsConversationSummary()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(homeDirectory);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/api/conversations",
            new CreateConversationRequest("Research"));
        ConversationResponse? created = await createResponse.Content.ReadFromJsonAsync<ConversationResponse>();
        ConversationListResponse? list = await client.GetFromJsonAsync<ConversationListResponse>("/api/conversations");

        Assert.NotNull(created);
        Assert.NotNull(list);
        Assert.Equal("Research", created.Title);
        Assert.Equal(created.Id, Assert.Single(list.Conversations).Id);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task RenameConversationUpdatesConversationTitle()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(homeDirectory);
        using HttpClient client = factory.CreateClient();
        ConversationResponse conversation = await CreateConversationAsync(client);

        using HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/conversations/{conversation.Id}",
            new UpdateConversationRequest("Renamed"));
        ConversationResponse? renamed = await response.Content.ReadFromJsonAsync<ConversationResponse>();
        ConversationListResponse? list = await client.GetFromJsonAsync<ConversationListResponse>("/api/conversations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(renamed);
        Assert.Equal("Renamed", renamed.Title);
        Assert.NotNull(list);
        Assert.Equal("Renamed", Assert.Single(list.Conversations).Title);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task DeleteConversationRemovesConversationAndMessages()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(homeDirectory);
        using HttpClient client = factory.CreateClient();
        ConversationResponse conversation = await CreateConversationAsync(client);
        await SendMessageAsync(client, conversation.Id);

        using HttpResponseMessage response = await client.DeleteAsync($"/api/conversations/{conversation.Id}");
        ConversationListResponse? list = await client.GetFromJsonAsync<ConversationListResponse>("/api/conversations");
        using HttpResponseMessage messagesResponse = await client.GetAsync($"/api/conversations/{conversation.Id}/messages");
        ErrorResponse? error = await messagesResponse.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotNull(list);
        Assert.Empty(list.Conversations);
        Assert.Equal(HttpStatusCode.NotFound, messagesResponse.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("conversation_not_found", error.Code);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task RenameConversationReturnsNotFoundForMissingConversation()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(homeDirectory);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PatchAsJsonAsync(
            $"/api/conversations/{Guid.NewGuid()}",
            new UpdateConversationRequest("Renamed"));
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("conversation_not_found", error.Code);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task DeleteConversationReturnsNotFoundForMissingConversation()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(homeDirectory);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.DeleteAsync($"/api/conversations/{Guid.NewGuid()}");
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("conversation_not_found", error.Code);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task SendMessagePersistsUserAndPendingAssistantMessages()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(homeDirectory);
        using HttpClient client = factory.CreateClient();
        ConversationResponse conversation = await CreateConversationAsync(client);

        using HttpResponseMessage sendHttpResponse = await client.PostAsJsonAsync(
            $"/api/conversations/{conversation.Id}/messages",
            new SendMessageRequest("fake-chat", "Hello"));
        SendMessageResponse? sendResponse = await sendHttpResponse.Content.ReadFromJsonAsync<SendMessageResponse>();
        MessageListResponse? messages = await client.GetFromJsonAsync<MessageListResponse>(
            $"/api/conversations/{conversation.Id}/messages");

        Assert.NotNull(sendResponse);
        Assert.NotNull(messages);
        Assert.Equal(2, messages.Messages.Count);
        Assert.Equal(sendResponse.UserMessageId, messages.Messages[0].Id);
        Assert.Equal("User", messages.Messages[0].Role);
        Assert.Equal(sendResponse.AssistantMessageId, messages.Messages[1].Id);
        Assert.Equal("Pending", messages.Messages[1].Status);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task StreamMessageWritesEventsAndPersistsCompletedAssistantMessage()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(homeDirectory);
        using HttpClient client = factory.CreateClient();
        ConversationResponse conversation = await CreateConversationAsync(client);
        SendMessageResponse sendResponse = await SendMessageAsync(client, conversation.Id);

        using HttpResponseMessage streamResponse = await client.PostAsJsonAsync(
            $"/api/conversations/{conversation.Id}/messages/{sendResponse.AssistantMessageId}/stream",
            new StreamMessageRequest("fake-chat"));
        string stream = await streamResponse.Content.ReadAsStringAsync();
        MessageListResponse? messages = await client.GetFromJsonAsync<MessageListResponse>(
            $"/api/conversations/{conversation.Id}/messages");

        Assert.Equal(HttpStatusCode.OK, streamResponse.StatusCode);
        Assert.Contains("Fake response", stream, StringComparison.Ordinal);
        Assert.NotNull(messages);
        ChatMessageResponse assistant = messages.Messages.Single(message => message.Id == sendResponse.AssistantMessageId);
        Assert.Equal("Completed", assistant.Status);
        Assert.Contains("Fake response", Assert.Single(assistant.Parts).Text, StringComparison.Ordinal);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task CancelMessageMarksAssistantMessageCancelled()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(homeDirectory);
        using HttpClient client = factory.CreateClient();
        ConversationResponse conversation = await CreateConversationAsync(client);
        SendMessageResponse sendResponse = await SendMessageAsync(client, conversation.Id);

        using HttpResponseMessage response = await client.PostAsync(
            $"/api/conversations/{conversation.Id}/messages/{sendResponse.AssistantMessageId}/cancel",
            content: null);
        MessageListResponse? messages = await client.GetFromJsonAsync<MessageListResponse>(
            $"/api/conversations/{conversation.Id}/messages");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(messages);
        ChatMessageResponse assistant = messages.Messages.Single(message => message.Id == sendResponse.AssistantMessageId);
        Assert.Equal("Cancelled", assistant.Status);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task SendMessageReturnsNotFoundForMissingConversation()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(homeDirectory);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/conversations/{Guid.NewGuid()}/messages",
            new SendMessageRequest("fake-chat", "Hello"));
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("conversation_not_found", error.Code);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task SendMessageReturnsBadRequestForMissingModel()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(homeDirectory);
        using HttpClient client = factory.CreateClient();
        ConversationResponse conversation = await CreateConversationAsync(client);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/conversations/{conversation.Id}/messages",
            new SendMessageRequest("missing-chat", "Hello"));
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        MessageListResponse? messages = await client.GetFromJsonAsync<MessageListResponse>(
            $"/api/conversations/{conversation.Id}/messages");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("model_not_found", error.Code);
        Assert.NotNull(messages);
        Assert.Empty(messages.Messages);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task SendMessageReturnsServiceUnavailableForUnavailableProvider()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(homeDirectory, new UnavailableChatModelProvider());
        using HttpClient client = factory.CreateClient();
        ConversationResponse conversation = await CreateConversationAsync(client);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/conversations/{conversation.Id}/messages",
            new SendMessageRequest("fake-chat", "Hello"));
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("provider_unavailable", error.Code);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task StreamMessageReturnsNotFoundForMissingAssistantMessage()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(homeDirectory);
        using HttpClient client = factory.CreateClient();
        ConversationResponse conversation = await CreateConversationAsync(client);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/conversations/{conversation.Id}/messages/{Guid.NewGuid()}/stream",
            new StreamMessageRequest("fake-chat"));
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("message_not_found", error.Code);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    private static async Task<ConversationResponse> CreateConversationAsync(HttpClient client)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/conversations",
            new CreateConversationRequest("Research"));
        ConversationResponse? conversation = await response.Content.ReadFromJsonAsync<ConversationResponse>();

        return conversation ?? throw new InvalidOperationException("Conversation response was missing.");
    }

    private static async Task<SendMessageResponse> SendMessageAsync(HttpClient client, Guid conversationId)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/conversations/{conversationId}/messages",
            new SendMessageRequest("fake-chat", "Hello"));
        SendMessageResponse? sendResponse = await response.Content.ReadFromJsonAsync<SendMessageResponse>();

        return sendResponse ?? throw new InvalidOperationException("Send message response was missing.");
    }

    private static string CreateTemporaryHomeDirectory() =>
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    private static void DeleteDirectoryIfItExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private sealed class ZaphiraServerApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string homeDirectory;
        private readonly IChatModelProvider chatModelProvider;

        public ZaphiraServerApplicationFactory(string homeDirectory)
            : this(homeDirectory, new FakeChatModelProvider())
        {
        }

        public ZaphiraServerApplicationFactory(string homeDirectory, IChatModelProvider chatModelProvider)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(homeDirectory);
            ArgumentNullException.ThrowIfNull(chatModelProvider);

            this.homeDirectory = homeDirectory;
            this.chatModelProvider = chatModelProvider;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration(configurationBuilder =>
            {
                configurationBuilder.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Zaphira:HomeDirectory"] = homeDirectory
                    });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IChatModelProvider>();
                services.AddSingleton(chatModelProvider);
            });
        }
    }

    private sealed class FakeChatModelProvider : IChatModelProvider
    {
        public ProviderId Id { get; } = new("fake");

        public string DisplayName { get; } = "Fake Provider";

        public ProviderCapabilities Capabilities { get; } =
            new([ProviderCapability.TextGeneration, ProviderCapability.StreamingGeneration]);

        public Task<ProviderModelCatalog> ListModelsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(new ProviderModelCatalog(
                Id,
                [new ProviderModelSummary(new ModelId("fake-chat"), "Fake Chat", Capabilities)]));
        }

        public async IAsyncEnumerable<ProviderGenerationEvent> GenerateAsync(
            ProviderGenerationRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Yield();
            yield return new TextGenerationDeltaEvent("Fake response");
            yield return GenerationCompletedEvent.Instance;
        }
    }

    private sealed class UnavailableChatModelProvider : IChatModelProvider
    {
        public ProviderId Id { get; } = new("unavailable");

        public string DisplayName { get; } = "Unavailable Provider";

        public ProviderCapabilities Capabilities { get; } =
            new([ProviderCapability.TextGeneration, ProviderCapability.StreamingGeneration]);

        public Task<ProviderModelCatalog> ListModelsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            throw new HttpRequestException("Provider is unavailable.");
        }

        public async IAsyncEnumerable<ProviderGenerationEvent> GenerateAsync(
            ProviderGenerationRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Yield();
            yield return new GenerationFailedEvent(new ProviderError(
                "Provider.Unavailable",
                "Provider is unavailable.",
                "Start the provider and try again."));
        }
    }
}
