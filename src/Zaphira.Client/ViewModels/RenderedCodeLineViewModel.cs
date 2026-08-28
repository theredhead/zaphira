namespace Zaphira.Client.ViewModels;

public sealed class RenderedCodeLineViewModel
{
    public RenderedCodeLineViewModel(IEnumerable<RenderedCodeTokenViewModel> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        RenderedCodeTokenViewModel[] materializedTokens = tokens.ToArray();
        if (materializedTokens.Any(token => token is null))
        {
            throw new ArgumentException("Code tokens cannot contain null values.", nameof(tokens));
        }

        Tokens = materializedTokens.Length == 0
            ? [new RenderedCodeTokenViewModel(string.Empty, RenderedCodeTokenKind.Plain)]
            : materializedTokens;
    }

    public IReadOnlyList<RenderedCodeTokenViewModel> Tokens { get; }
}
