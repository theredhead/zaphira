using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Zaphira.Client.Chat;
using Zaphira.Contracts;

namespace Zaphira.Client.ViewModels;

public partial class ChatWorkspaceViewModel : ViewModelBase
{
    private const string DefaultModelId = "llama3.2";

    private readonly IChatApiClient chatApiClient;
    private readonly CancellationTokenSource lifetimeCancellation = new();
    private CancellationTokenSource activeGenerationCancellation = new();
    private ConversationItemViewModel selectedConversation = ConversationItemViewModel.Empty;
    private string composerText = string.Empty;
    private string selectedConversationTitle = string.Empty;
    private string statusText = "Ready";
    private string activeModelId = DefaultModelId;
    private bool isLoading;
    private bool isStreaming;
    private bool isConfirmingDelete;

    public ChatWorkspaceViewModel(IChatApiClient chatApiClient)
    {
        ArgumentNullException.ThrowIfNull(chatApiClient);

        this.chatApiClient = chatApiClient;
        Conversations = [];
        Messages = [];
    }

    public ObservableCollection<ConversationItemViewModel> Conversations { get; }

    public ObservableCollection<ChatMessageViewModel> Messages { get; }

    public Task<ModelListResponse> GetInstalledModelsAsync(CancellationToken cancellationToken) =>
        chatApiClient.GetInstalledModelsAsync(cancellationToken);

    public ConversationItemViewModel SelectedConversation
    {
        get => selectedConversation;
        private set
        {
            if (SetProperty(ref selectedConversation, value))
            {
                SelectedConversationTitle = value.HasConversation ? value.Title : string.Empty;
                IsConfirmingDelete = false;
                OnPropertyChanged(nameof(HasSelectedConversation));
            }
        }
    }

    public bool HasSelectedConversation => SelectedConversation.HasConversation;

    public string ComposerText
    {
        get => composerText;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            SetProperty(ref composerText, value);
        }
    }

    public string SelectedConversationTitle
    {
        get => selectedConversationTitle;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            SetProperty(ref selectedConversationTitle, value);
        }
    }

    public string StatusText
    {
        get => statusText;
        private set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            SetProperty(ref statusText, value);
        }
    }

    public string ActiveModelId
    {
        get => activeModelId;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            SetProperty(ref activeModelId, value);
        }
    }

    public bool IsLoading
    {
        get => isLoading;
        private set => SetProperty(ref isLoading, value);
    }

    public bool IsStreaming
    {
        get => isStreaming;
        private set => SetProperty(ref isStreaming, value);
    }

    public bool IsConfirmingDelete
    {
        get => isConfirmingDelete;
        private set => SetProperty(ref isConfirmingDelete, value);
    }

    [RelayCommand]
    public void StartNewConversation()
    {
        SelectedConversation = ConversationItemViewModel.Empty;
        Messages.Clear();
        ComposerText = string.Empty;
        StatusText = "Ready";
    }

    [RelayCommand]
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        StatusText = "Loading";

        try
        {
            IReadOnlyList<ConversationResponse> conversations = await chatApiClient.GetConversationsAsync(cancellationToken);
            Conversations.Clear();
            foreach (ConversationResponse conversation in conversations)
            {
                Conversations.Add(ConversationItemViewModel.FromResponse(conversation));
            }

            if (Conversations.Count > 0)
            {
                await SelectConversationAsync(Conversations[0], cancellationToken);
            }
            else
            {
                SelectedConversation = ConversationItemViewModel.Empty;
                Messages.Clear();
            }

            StatusText = "Ready";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusText = ToStatusText(exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SelectConversationAsync(ConversationItemViewModel conversation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        if (!conversation.HasConversation)
        {
            SelectedConversation = ConversationItemViewModel.Empty;
            Messages.Clear();
            StatusText = "Ready";
            return;
        }

        SelectedConversation = conversation;
        IReadOnlyList<ChatMessageResponse> messages = await chatApiClient.GetMessagesAsync(conversation.Id, cancellationToken);
        Messages.Clear();
        foreach (ChatMessageResponse message in messages)
        {
            Messages.Add(ChatMessageViewModel.FromResponse(message));
        }

        StatusText = "Ready";
    }

    [RelayCommand]
    public async Task RenameConversationAsync(CancellationToken cancellationToken)
    {
        if (!HasSelectedConversation)
        {
            return;
        }

        string title = SelectedConversationTitle.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            StatusText = "Conversation title is required.";
            return;
        }

        try
        {
            ConversationResponse response = await chatApiClient.RenameConversationAsync(
                SelectedConversation.Id,
                title,
                cancellationToken);
            SelectedConversation.UpdateFromResponse(response);
            SelectedConversationTitle = response.Title;
            StatusText = "Renamed";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusText = ToStatusText(exception);
        }
    }

    [RelayCommand]
    public void RequestDeleteConversation()
    {
        if (!HasSelectedConversation)
        {
            return;
        }

        IsConfirmingDelete = true;
        StatusText = "Confirm delete to remove this conversation.";
    }

    [RelayCommand]
    public void CancelDeleteConversation()
    {
        IsConfirmingDelete = false;
        StatusText = "Ready";
    }

    [RelayCommand]
    public async Task ConfirmDeleteConversationAsync(CancellationToken cancellationToken)
    {
        if (!HasSelectedConversation || !IsConfirmingDelete)
        {
            return;
        }

        try
        {
            ConversationItemViewModel conversationToDelete = SelectedConversation;
            await chatApiClient.DeleteConversationAsync(conversationToDelete.Id, cancellationToken);
            Conversations.Remove(conversationToDelete);
            Messages.Clear();
            if (Conversations.Count > 0)
            {
                await SelectConversationAsync(Conversations[0], cancellationToken);
            }
            else
            {
                SelectedConversation = ConversationItemViewModel.Empty;
            }

            StatusText = "Deleted";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusText = ToStatusText(exception);
        }
    }

    [RelayCommand]
    public async Task SendMessageAsync(CancellationToken cancellationToken)
    {
        string text = ComposerText.Trim();
        if (string.IsNullOrWhiteSpace(text) || IsStreaming)
        {
            return;
        }

        CancellationToken linkedCancellationToken = cancellationToken == CancellationToken.None
            ? lifetimeCancellation.Token
            : cancellationToken;

        try
        {
            ConversationItemViewModel conversation = await EnsureConversationAsync(linkedCancellationToken);
            SendMessageResponse sendResponse = await chatApiClient.SendMessageAsync(
                conversation.Id,
                ActiveModelId,
                text,
                linkedCancellationToken);

            ComposerText = string.Empty;
            ChatMessageViewModel userMessage = ChatMessageViewModel.User(sendResponse.UserMessageId, text);
            ChatMessageViewModel assistantMessage = ChatMessageViewModel.PendingAssistant(sendResponse.AssistantMessageId);
            Messages.Add(userMessage);
            Messages.Add(assistantMessage);
            StatusText = "Streaming";
            IsStreaming = true;

            activeGenerationCancellation.Dispose();
            activeGenerationCancellation = CancellationTokenSource.CreateLinkedTokenSource(linkedCancellationToken);
            await foreach (GenerationStreamResponse streamResponse in chatApiClient.StreamMessageAsync(
                conversation.Id,
                sendResponse.AssistantMessageId,
                ActiveModelId,
                activeGenerationCancellation.Token))
            {
                ApplyStreamResponse(assistantMessage, streamResponse);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusText = ToStatusText(exception);
        }
        finally
        {
            IsStreaming = false;
        }
    }

    [RelayCommand]
    public async Task StopGenerationAsync(CancellationToken cancellationToken)
    {
        if (!IsStreaming)
        {
            return;
        }

        activeGenerationCancellation.Cancel();
        ChatMessageViewModel assistantMessage = Messages.LastOrDefault(message => message.Role == "Assistant")
            ?? ChatMessageViewModel.PendingAssistant(Guid.NewGuid());

        if (HasSelectedConversation && assistantMessage.Id != Guid.Empty)
        {
            await chatApiClient.CancelMessageAsync(SelectedConversation.Id, assistantMessage.Id, cancellationToken);
        }

        assistantMessage.ChangeStatus("Cancelled");
        StatusText = "Cancelled";
        IsStreaming = false;
    }

    private async Task<ConversationItemViewModel> EnsureConversationAsync(CancellationToken cancellationToken)
    {
        if (HasSelectedConversation)
        {
            return SelectedConversation;
        }

        ConversationResponse createdConversation = await chatApiClient.CreateConversationAsync("New chat", cancellationToken);
        ConversationItemViewModel conversation = ConversationItemViewModel.FromResponse(createdConversation);
        Conversations.Insert(0, conversation);
        SelectedConversation = conversation;

        return conversation;
    }

    private void ApplyStreamResponse(ChatMessageViewModel assistantMessage, GenerationStreamResponse streamResponse)
    {
        ArgumentNullException.ThrowIfNull(assistantMessage);
        ArgumentNullException.ThrowIfNull(streamResponse);

        switch (streamResponse.Kind)
        {
            case "text_delta":
                assistantMessage.AppendText(streamResponse.Text);
                assistantMessage.ChangeStatus("Streaming");
                break;
            case "completed":
                assistantMessage.ChangeStatus("Completed");
                StatusText = "Ready";
                break;
            case "failed":
                assistantMessage.ChangeStatus("Failed");
                StatusText = streamResponse.Text;
                break;
            case "cancelled":
                assistantMessage.ChangeStatus("Cancelled");
                StatusText = "Cancelled";
                break;
            default:
                assistantMessage.AppendText($"[{streamResponse.Kind}] {streamResponse.Text}");
                break;
        }
    }

    private static string ToStatusText(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception is ChatApiException chatApiException
            ? $"{chatApiException.Error.Message} {chatApiException.Error.Suggestion}"
            : exception.Message;
    }
}
