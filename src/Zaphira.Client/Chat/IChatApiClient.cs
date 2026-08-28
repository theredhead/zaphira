using Zaphira.Contracts;

namespace Zaphira.Client.Chat;

public interface IChatApiClient
{
    Task<ModelListResponse> GetInstalledModelsAsync(CancellationToken cancellationToken);

    Task SelectActiveModelAsync(string modelId, CancellationToken cancellationToken);

    IAsyncEnumerable<ModelInstallationStreamResponse> InstallModelAsync(
        string modelId,
        CancellationToken cancellationToken);

    Task RemoveModelAsync(string modelId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ConversationResponse>> GetConversationsAsync(CancellationToken cancellationToken);

    Task<ConversationResponse> CreateConversationAsync(string title, CancellationToken cancellationToken);

    Task<ConversationResponse> RenameConversationAsync(
        Guid conversationId,
        string title,
        CancellationToken cancellationToken);

    Task DeleteConversationAsync(Guid conversationId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ChatMessageResponse>> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken);

    Task<SendMessageResponse> SendMessageAsync(
        Guid conversationId,
        string modelId,
        string text,
        CancellationToken cancellationToken);

    IAsyncEnumerable<GenerationStreamResponse> StreamMessageAsync(
        Guid conversationId,
        Guid assistantMessageId,
        string modelId,
        CancellationToken cancellationToken);

    Task CancelMessageAsync(Guid conversationId, Guid assistantMessageId, CancellationToken cancellationToken);
}
