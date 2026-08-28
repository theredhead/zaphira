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
        new(id, "Assistant", "Pending", []);

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
            foreach (RenderedMessagePartViewModel renderedPart in ParseMarkdownBlocks(part))
            {
                RenderedParts.Add(renderedPart);
            }
        }
    }

    private static IReadOnlyList<RenderedMessagePartViewModel> ParseMarkdownBlocks(string text)
    {
        List<RenderedMessagePartViewModel> renderedParts = [];
        int cursor = 0;

        while (cursor < text.Length)
        {
            int fenceStart = text.IndexOf("```", cursor, StringComparison.Ordinal);
            if (fenceStart < 0)
            {
                AddMarkdownTextBlocks(renderedParts, text[cursor..]);
                break;
            }

            AddMarkdownTextBlocks(renderedParts, text[cursor..fenceStart]);
            int languageStart = fenceStart + 3;
            int firstLineEnd = text.IndexOf('\n', languageStart);
            if (firstLineEnd < 0)
            {
                AddMarkdownTextBlocks(renderedParts, text[fenceStart..]);
                break;
            }

            string language = text[languageStart..firstLineEnd].Trim();
            int codeStart = firstLineEnd + 1;
            int fenceEnd = text.IndexOf("```", codeStart, StringComparison.Ordinal);
            if (fenceEnd < 0)
            {
                AddMarkdownTextBlocks(renderedParts, text[fenceStart..]);
                break;
            }

            string code = text[codeStart..fenceEnd].TrimEnd();
            renderedParts.Add(RenderedMessagePartViewModel.CodeBlock(code, language));
            cursor = fenceEnd + 3;
        }

        if (renderedParts.Count == 0)
        {
            renderedParts.Add(RenderedMessagePartViewModel.Paragraph(string.Empty));
        }

        return renderedParts;
    }

    private static void AddMarkdownTextBlocks(List<RenderedMessagePartViewModel> renderedParts, string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        List<string> paragraphLines = [];
        string normalizedText = text.ReplaceLineEndings("\n");
        foreach (string line in normalizedText.Split('\n'))
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine))
            {
                FlushParagraph(renderedParts, paragraphLines);
                continue;
            }

            if (TryParseHeading(trimmedLine, out int headingLevel, out string headingText))
            {
                FlushParagraph(renderedParts, paragraphLines);
                renderedParts.Add(RenderedMessagePartViewModel.Heading(headingText, headingLevel));
                continue;
            }

            if (TryParseListItem(trimmedLine, out string listItemText))
            {
                FlushParagraph(renderedParts, paragraphLines);
                renderedParts.Add(RenderedMessagePartViewModel.ListItem(listItemText));
                continue;
            }

            if (TryParseQuote(trimmedLine, out string quoteText))
            {
                FlushParagraph(renderedParts, paragraphLines);
                renderedParts.Add(RenderedMessagePartViewModel.Quote(quoteText));
                continue;
            }

            paragraphLines.Add(trimmedLine);
        }

        FlushParagraph(renderedParts, paragraphLines);
    }

    private static void FlushParagraph(
        List<RenderedMessagePartViewModel> renderedParts,
        List<string> paragraphLines)
    {
        if (paragraphLines.Count == 0)
        {
            return;
        }

        renderedParts.Add(RenderedMessagePartViewModel.Paragraph(string.Join(" ", paragraphLines)));
        paragraphLines.Clear();
    }

    private static bool TryParseHeading(string line, out int headingLevel, out string headingText)
    {
        headingLevel = 0;
        headingText = string.Empty;

        int cursor = 0;
        while (cursor < line.Length && line[cursor] == '#')
        {
            cursor++;
        }

        if (cursor is < 1 or > 6 || cursor >= line.Length || line[cursor] != ' ')
        {
            return false;
        }

        string parsedText = line[(cursor + 1)..].Trim();
        if (string.IsNullOrEmpty(parsedText))
        {
            return false;
        }

        headingLevel = cursor;
        headingText = parsedText;

        return true;
    }

    private static bool TryParseListItem(string line, out string listItemText)
    {
        listItemText = string.Empty;

        if (line.Length < 3 || line[1] != ' ')
        {
            return false;
        }

        char marker = line[0];
        if (marker is not ('-' or '*' or '+'))
        {
            return false;
        }

        listItemText = line[2..].Trim();

        return !string.IsNullOrEmpty(listItemText);
    }

    private static bool TryParseQuote(string line, out string quoteText)
    {
        quoteText = string.Empty;

        if (!line.StartsWith('>'))
        {
            return false;
        }

        quoteText = line[1..].Trim();

        return !string.IsNullOrEmpty(quoteText);
    }
}
