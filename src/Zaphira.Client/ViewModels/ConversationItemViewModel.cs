using Zaphira.Contracts;

namespace Zaphira.Client.ViewModels;

public sealed class ConversationItemViewModel : ViewModelBase
{
    public ConversationItemViewModel(Guid id, string title, string preview, int messageCount)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Conversation id cannot be empty.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(preview);
        ArgumentOutOfRangeException.ThrowIfNegative(messageCount);

        Id = id;
        Title = title;
        Preview = preview;
        MessageCount = messageCount;
    }

    private ConversationItemViewModel()
    {
        Id = Guid.Empty;
        Title = "No conversation";
        Preview = string.Empty;
        MessageCount = 0;
    }

    public Guid Id { get; }

    public string Title { get; }

    public string Preview { get; }

    public int MessageCount { get; }

    public bool HasConversation => Id != Guid.Empty;

    public static ConversationItemViewModel Empty { get; } = new();

    public static ConversationItemViewModel FromResponse(ConversationResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new ConversationItemViewModel(response.Id, response.Title, response.Preview, response.MessageCount);
    }
}
