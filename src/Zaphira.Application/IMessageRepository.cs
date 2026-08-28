using Zaphira.Domain;

namespace Zaphira.Application;

public interface IMessageRepository
{
    Task SaveAsync(ChatMessage message, CancellationToken cancellationToken);

    Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(ConversationId conversationId, CancellationToken cancellationToken);
}
