using System.Collections.ObjectModel;
using Zaphira.Contracts;

namespace Zaphira.Client.ViewModels;

public sealed class ChatMessageViewModel : ViewModelBase
{
    private readonly List<string> textParts = [];
    private string status;

    public ChatMessageViewModel(
        Guid id,
        string role,
        string status,
        IEnumerable<MessagePartResponse> parts)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Message id cannot be empty.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        ArgumentNullException.ThrowIfNull(parts);

        Id = id;
        Role = role;
        this.status = status;
        RenderedParts = [];

        MessagePartResponse[] materializedParts = parts.ToArray();
        if (materializedParts.Any(part => part is null))
        {
            throw new ArgumentException("Message parts cannot contain null values.", nameof(parts));
        }

        foreach (MessagePartResponse part in materializedParts)
        {
            AddPart(part);
        }
    }

    public Guid Id { get; }

    public string Role { get; }

    public string Status
    {
        get => status;
        private set => SetProperty(ref status, value);
    }

    public ObservableCollection<RenderedMessagePartViewModel> RenderedParts { get; }

    public string DisplayText => string.Join(Environment.NewLine, textParts);

    public void AppendText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (textParts.Count == 0)
        {
            textParts.Add(text);
        }
        else
        {
            textParts[^1] += text;
        }

        RebuildRenderedParts();
        OnPropertyChanged(nameof(DisplayText));
    }

    public void ChangeStatus(string newStatus)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newStatus);

        Status = newStatus;
    }

    public static ChatMessageViewModel PendingAssistant(Guid id) =>
        new(id, "Assistant", "Pending", [new MessagePartResponse("text", string.Empty)]);

    public static ChatMessageViewModel User(Guid id, string text) =>
        new(id, "User", "Completed", [new MessagePartResponse("text", text)]);

    public static ChatMessageViewModel FromResponse(ChatMessageResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return new ChatMessageViewModel(response.Id, response.Role, response.Status, response.Parts);
    }

    private void AddPart(MessagePartResponse part)
    {
        if (part.Kind.Equals("text", StringComparison.OrdinalIgnoreCase))
        {
            textParts.Add(part.Text);
        }
        else
        {
            textParts.Add($"[{part.Kind}] {part.Text}");
        }

        RebuildRenderedParts();
        OnPropertyChanged(nameof(DisplayText));
    }

    private void RebuildRenderedParts()
    {
        RenderedParts.Clear();

        foreach (string part in textParts)
        {
            foreach (RenderedMessagePartViewModel renderedPart in SplitMarkdownCodeBlocks(part))
            {
                RenderedParts.Add(renderedPart);
            }
        }
    }

    private static IReadOnlyList<RenderedMessagePartViewModel> SplitMarkdownCodeBlocks(string text)
    {
        List<RenderedMessagePartViewModel> renderedParts = [];
        int cursor = 0;

        while (cursor < text.Length)
        {
            int fenceStart = text.IndexOf("```", cursor, StringComparison.Ordinal);
            if (fenceStart < 0)
            {
                AddTextPart(renderedParts, text[cursor..]);
                break;
            }

            AddTextPart(renderedParts, text[cursor..fenceStart]);
            int languageStart = fenceStart + 3;
            int firstLineEnd = text.IndexOf('\n', languageStart);
            if (firstLineEnd < 0)
            {
                AddTextPart(renderedParts, text[fenceStart..]);
                break;
            }

            string language = text[languageStart..firstLineEnd].Trim();
            int codeStart = firstLineEnd + 1;
            int fenceEnd = text.IndexOf("```", codeStart, StringComparison.Ordinal);
            if (fenceEnd < 0)
            {
                AddTextPart(renderedParts, text[fenceStart..]);
                break;
            }

            string code = text[codeStart..fenceEnd].TrimEnd();
            renderedParts.Add(new RenderedMessagePartViewModel(code, isCodeBlock: true, language));
            cursor = fenceEnd + 3;
        }

        if (renderedParts.Count == 0)
        {
            renderedParts.Add(new RenderedMessagePartViewModel(string.Empty, isCodeBlock: false, string.Empty));
        }

        return renderedParts;
    }

    private static void AddTextPart(List<RenderedMessagePartViewModel> renderedParts, string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        renderedParts.Add(new RenderedMessagePartViewModel(text.Trim(), isCodeBlock: false, string.Empty));
    }
}
