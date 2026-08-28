namespace Zaphira.Client.ViewModels;

public sealed class RenderedCodeTokenViewModel
{
    public RenderedCodeTokenViewModel(string text, RenderedCodeTokenKind kind)
    {
        ArgumentNullException.ThrowIfNull(text);

        Text = text;
        Kind = kind;
    }

    public string Text { get; }

    public RenderedCodeTokenKind Kind { get; }

    public bool IsPlain => Kind is RenderedCodeTokenKind.Plain or RenderedCodeTokenKind.NumberLiteral;

    public bool IsKeyword => Kind == RenderedCodeTokenKind.Keyword;

    public bool IsStringLiteral => Kind == RenderedCodeTokenKind.StringLiteral;

    public bool IsComment => Kind == RenderedCodeTokenKind.Comment;
}
