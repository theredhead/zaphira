using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Zaphira.Client.Views;

public sealed class MarkdownTextBlock : TextBlock
{
    public static readonly StyledProperty<string> MarkdownProperty =
        AvaloniaProperty.Register<MarkdownTextBlock, string>(nameof(Markdown), string.Empty);

    public string Markdown
    {
        get => GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    static MarkdownTextBlock()
    {
        MarkdownProperty.Changed.AddClassHandler<MarkdownTextBlock>((textBlock, _) => textBlock.RebuildInlines());
    }

    public MarkdownTextBlock()
    {
        TextWrapping = TextWrapping.Wrap;
        RebuildInlines();
    }

    private void RebuildInlines()
    {
        Inlines ??= [];
        Inlines.Clear();

        foreach (Inline inline in ParseInlineMarkdown(Markdown))
        {
            Inlines.Add(inline);
        }
    }

    private static IReadOnlyList<Inline> ParseInlineMarkdown(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        List<Inline> inlines = [];
        int cursor = 0;

        while (cursor < markdown.Length)
        {
            InlineMatch nextMatch = FindNextMatch(markdown, cursor);
            if (!nextMatch.WasFound)
            {
                AddRun(inlines, markdown[cursor..]);
                break;
            }

            AddRun(inlines, markdown[cursor..nextMatch.Start]);
            string content = markdown[(nextMatch.Start + nextMatch.MarkerLength)..nextMatch.End];
            AddStyledInline(inlines, content, nextMatch.Kind);
            cursor = nextMatch.End + nextMatch.MarkerLength;
        }

        if (inlines.Count == 0)
        {
            AddRun(inlines, string.Empty);
        }

        return inlines;
    }

    private static InlineMatch FindNextMatch(string markdown, int startIndex)
    {
        InlineMatch code = FindDelimited(markdown, "`", InlineMarkdownKind.Code, startIndex);
        InlineMatch bold = FindDelimited(markdown, "**", InlineMarkdownKind.Bold, startIndex);
        InlineMatch italic = FindDelimited(markdown, "*", InlineMarkdownKind.Italic, startIndex);

        return new[] { code, bold, italic }
            .Where(match => match.WasFound)
            .OrderBy(match => match.Start)
            .ThenByDescending(match => match.MarkerLength)
            .FirstOrDefault(InlineMatch.NotFound);
    }

    private static InlineMatch FindDelimited(
        string markdown,
        string marker,
        InlineMarkdownKind kind,
        int startIndex)
    {
        int start = markdown.IndexOf(marker, startIndex, StringComparison.Ordinal);
        if (start < 0)
        {
            return InlineMatch.NotFound;
        }

        int contentStart = start + marker.Length;
        int end = markdown.IndexOf(marker, contentStart, StringComparison.Ordinal);
        if (end <= contentStart)
        {
            return InlineMatch.NotFound;
        }

        return new InlineMatch(true, start, end, marker.Length, kind);
    }

    private static void AddStyledInline(List<Inline> inlines, string text, InlineMarkdownKind kind)
    {
        if (kind == InlineMarkdownKind.Bold)
        {
            Bold bold = new();
            bold.Inlines.Add(new Run(text));
            inlines.Add(bold);
            return;
        }

        if (kind == InlineMarkdownKind.Italic)
        {
            Italic italic = new();
            italic.Inlines.Add(new Run(text));
            inlines.Add(italic);
            return;
        }

        inlines.Add(new Run(text)
        {
            FontFamily = new FontFamily("Consolas, Menlo, Monaco, monospace")
        });
    }

    private static void AddRun(List<Inline> inlines, string text)
    {
        if (text.Length > 0)
        {
            inlines.Add(new Run(text));
        }
    }

    private enum InlineMarkdownKind
    {
        Bold = 0,
        Italic = 1,
        Code = 2
    }

    private readonly record struct InlineMatch(
        bool WasFound,
        int Start,
        int End,
        int MarkerLength,
        InlineMarkdownKind Kind)
    {
        public static InlineMatch NotFound { get; } = new(false, 0, 0, 0, InlineMarkdownKind.Code);
    }
}
