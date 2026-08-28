using Zaphira.Domain;

namespace Zaphira.Application;

public interface IConversationRepository
{
    Task SaveAsync(ConversationSummary summary, CancellationToken cancellationToken);

    Task<IReadOnlyList<ConversationSummary>> GetSummariesAsync(CancellationToken cancellationToken);
}
