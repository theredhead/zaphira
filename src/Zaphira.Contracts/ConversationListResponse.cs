namespace Zaphira.Contracts;

public sealed record ConversationListResponse
{
    public ConversationListResponse(IReadOnlyList<ConversationResponse> conversations)
    {
        ArgumentNullException.ThrowIfNull(conversations);

        ConversationResponse[] materializedConversations = conversations.ToArray();
        if (materializedConversations.Any(conversation => conversation is null))
        {
            throw new ArgumentException("Conversations cannot contain null values.", nameof(conversations));
        }

        Conversations = materializedConversations;
    }

    public IReadOnlyList<ConversationResponse> Conversations { get; }
}
