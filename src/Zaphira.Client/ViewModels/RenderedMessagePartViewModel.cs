namespace Zaphira.Client.ViewModels;

public sealed class RenderedMessagePartViewModel : ViewModelBase
{
    private RenderedMessagePartViewModel(
        string text,
        RenderedMessagePartKind kind,
        int headingLevel,
        string language,
        IReadOnlyList<RenderedCodeLineViewModel> codeLines)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(language);
        ArgumentNullException.ThrowIfNull(codeLines);

        Text = text;
        Kind = kind;
        HeadingLevel = headingLevel;
        Language = language;
        CodeLines = codeLines;
    }

    public string Text { get; }

    public RenderedMessagePartKind Kind { get; }

    public int HeadingLevel { get; }

    public bool IsParagraph => Kind == RenderedMessagePartKind.Paragraph;

    public bool IsHeading => Kind == RenderedMessagePartKind.Heading;

    public bool IsHeadingOne => IsHeading && HeadingLevel == 1;

    public bool IsHeadingTwo => IsHeading && HeadingLevel == 2;

    public bool IsHeadingThree => IsHeading && HeadingLevel >= 3;

    public bool IsListItem => Kind == RenderedMessagePartKind.ListItem;

    public bool IsQuote => Kind == RenderedMessagePartKind.Quote;

    public bool IsCodeBlock => Kind == RenderedMessagePartKind.CodeBlock;

    public bool IsTextPart => IsParagraph;

    public string Language { get; }

    public string CodeHeader => string.IsNullOrWhiteSpace(Language) ? "code" : Language;

    public IReadOnlyList<RenderedCodeLineViewModel> CodeLines { get; }

    public static RenderedMessagePartViewModel Paragraph(string text) =>
        new(text, RenderedMessagePartKind.Paragraph, headingLevel: 0, string.Empty, []);

    public static RenderedMessagePartViewModel Heading(string text, int headingLevel)
    {
        if (headingLevel < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(headingLevel), "Heading level must be positive.");
        }

        return new(text, RenderedMessagePartKind.Heading, headingLevel, string.Empty, []);
    }

    public static RenderedMessagePartViewModel ListItem(string text) =>
        new(text, RenderedMessagePartKind.ListItem, headingLevel: 0, string.Empty, []);

    public static RenderedMessagePartViewModel Quote(string text) =>
        new(text, RenderedMessagePartKind.Quote, headingLevel: 0, string.Empty, []);

    public static RenderedMessagePartViewModel CodeBlock(string text, string language) =>
        new(
            text,
            RenderedMessagePartKind.CodeBlock,
            headingLevel: 0,
            language,
            SyntaxHighlightedCodeParser.Highlight(text, language));
}
