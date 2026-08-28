using Zaphira.Domain;

namespace Zaphira.Application;

public sealed record ConversationSummaryLookup
{
    private ConversationSummaryLookup(bool exists, ConversationSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        Exists = exists;
        Summary = summary;
    }

    public bool Exists { get; }

    public ConversationSummary Summary { get; }

    public static ConversationSummaryLookup Found(ConversationSummary summary) => new(exists: true, summary);

    public static ConversationSummaryLookup NotFound(ConversationId conversationId)
    {
        DateTimeOffset now = DateTimeOffset.UnixEpoch;

        return new ConversationSummaryLookup(
            exists: false,
            new ConversationSummary(
                conversationId,
                "Conversation not found",
                ConversationPreview.Empty(),
                messageCount: 0,
                now,
                now));
    }
}
