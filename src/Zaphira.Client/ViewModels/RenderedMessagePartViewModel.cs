namespace Zaphira.Client.ViewModels;

public sealed class RenderedMessagePartViewModel : ViewModelBase
{
    public RenderedMessagePartViewModel(string text, bool isCodeBlock, string language)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(language);

        Text = text;
        IsCodeBlock = isCodeBlock;
        Language = language;
    }

    public string Text { get; }

    public bool IsCodeBlock { get; }

    public bool IsTextPart => !IsCodeBlock;

    public string Language { get; }

    public string CodeHeader => string.IsNullOrWhiteSpace(Language) ? "code" : Language;
}
