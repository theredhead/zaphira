using Zaphira.Contracts;

namespace Zaphira.Client.ViewModels;

public sealed class ConversationItemViewModel : ViewModelBase
{
    private string title;
    private string preview;
    private int messageCount;

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
        this.title = title;
        this.preview = preview;
        this.messageCount = messageCount;
    }

    private ConversationItemViewModel()
    {
        Id = Guid.Empty;
        title = "No conversation";
        preview = string.Empty;
        messageCount = 0;
    }

    public Guid Id { get; }

    public string Title
    {
        get => title;
        private set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            SetProperty(ref title, value);
        }
    }

    public string Preview
    {
        get => preview;
        private set
        {
            ArgumentNullException.ThrowIfNull(value);
            SetProperty(ref preview, value);
        }
    }

    public int MessageCount
    {
        get => messageCount;
        private set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            SetProperty(ref messageCount, value);
        }
    }

    public bool HasConversation => Id != Guid.Empty;

    public static ConversationItemViewModel Empty { get; } = new();

    public static ConversationItemViewModel FromResponse(ConversationResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new ConversationItemViewModel(response.Id, response.Title, response.Preview, response.MessageCount);
    }

    public void UpdateFromResponse(ConversationResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.Id != Id)
        {
            throw new ArgumentException("Conversation response id must match the view model id.", nameof(response));
        }

        Title = response.Title;
        Preview = response.Preview;
        MessageCount = response.MessageCount;
    }
}
