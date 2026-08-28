using Zaphira.Domain;

namespace Zaphira.Application;

public interface IConversationRepository
{
    Task SaveAsync(ConversationSummary summary, CancellationToken cancellationToken);

    Task<ConversationSummaryLookup> GetSummaryAsync(ConversationId conversationId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ConversationSummary>> GetSummariesAsync(CancellationToken cancellationToken);

    Task<bool> DeleteAsync(ConversationId conversationId, CancellationToken cancellationToken);
}
