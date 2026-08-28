using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Zaphira.Contracts;

namespace Zaphira.Client.Chat;

public sealed class HttpChatApiClient : IChatApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;

    public HttpChatApiClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        this.httpClient = httpClient;
    }

    public async Task<IReadOnlyList<ConversationResponse>> GetConversationsAsync(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync("/api/conversations", cancellationToken);
        await ThrowIfErrorAsync(response, cancellationToken);

        ConversationListResponse body = await ReadRequiredJsonAsync<ConversationListResponse>(response, cancellationToken);

        return body.Conversations;
    }

    public async Task<ConversationResponse> CreateConversationAsync(string title, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "/api/conversations",
            new CreateConversationRequest(title),
            SerializerOptions,
            cancellationToken);
        await ThrowIfErrorAsync(response, cancellationToken);

        return await ReadRequiredJsonAsync<ConversationResponse>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<ChatMessageResponse>> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        ThrowIfEmpty(conversationId, nameof(conversationId));

        using HttpResponseMessage response = await httpClient.GetAsync(
            $"/api/conversations/{conversationId}/messages",
            cancellationToken);
        await ThrowIfErrorAsync(response, cancellationToken);

        MessageListResponse body = await ReadRequiredJsonAsync<MessageListResponse>(response, cancellationToken);

        return body.Messages;
    }

    public async Task<SendMessageResponse> SendMessageAsync(
        Guid conversationId,
        string modelId,
        string text,
        CancellationToken cancellationToken)
    {
        ThrowIfEmpty(conversationId, nameof(conversationId));
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            $"/api/conversations/{conversationId}/messages",
            new SendMessageRequest(modelId, text),
            SerializerOptions,
            cancellationToken);
        await ThrowIfErrorAsync(response, cancellationToken);

        return await ReadRequiredJsonAsync<SendMessageResponse>(response, cancellationToken);
    }

    public async IAsyncEnumerable<GenerationStreamResponse> StreamMessageAsync(
        Guid conversationId,
        Guid assistantMessageId,
        string modelId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ThrowIfEmpty(conversationId, nameof(conversationId));
        ThrowIfEmpty(assistantMessageId, nameof(assistantMessageId));
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);

        using HttpRequestMessage request = new(HttpMethod.Post, $"/api/conversations/{conversationId}/messages/{assistantMessageId}/stream")
        {
            Content = JsonContent.Create(new StreamMessageRequest(modelId), options: SerializerOptions)
        };
        using HttpResponseMessage response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        await ThrowIfErrorAsync(response, cancellationToken);

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using StreamReader reader = new(stream);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            GenerationStreamResponse? streamResponse = JsonSerializer.Deserialize<GenerationStreamResponse>(line, SerializerOptions);
            if (streamResponse is null)
            {
                throw new InvalidOperationException("The stream returned an empty event.");
            }

            yield return streamResponse;
        }
    }

    public async Task CancelMessageAsync(Guid conversationId, Guid assistantMessageId, CancellationToken cancellationToken)
    {
        ThrowIfEmpty(conversationId, nameof(conversationId));
        ThrowIfEmpty(assistantMessageId, nameof(assistantMessageId));

        using HttpResponseMessage response = await httpClient.PostAsync(
            $"/api/conversations/{conversationId}/messages/{assistantMessageId}/cancel",
            content: null,
            cancellationToken);
        await ThrowIfErrorAsync(response, cancellationToken);
    }

    private static async Task<T> ReadRequiredJsonAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
        where T : class
    {
        T? body = await response.Content.ReadFromJsonAsync<T>(SerializerOptions, cancellationToken);

        return body ?? throw new InvalidOperationException("The API returned an empty response body.");
    }

    private static async Task ThrowIfErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(SerializerOptions, cancellationToken);
        ErrorResponse nonNullError = error ?? ErrorResponse.UnexpectedServerError();

        throw new ChatApiException((int)response.StatusCode, nonNullError);
    }

    private static void ThrowIfEmpty(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier cannot be empty.", parameterName);
        }
    }
}
