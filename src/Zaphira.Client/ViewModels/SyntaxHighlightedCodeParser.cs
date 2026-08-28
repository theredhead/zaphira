namespace Zaphira.Client.ViewModels;

internal static class SyntaxHighlightedCodeParser
{
    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract",
        "async",
        "await",
        "bool",
        "break",
        "case",
        "catch",
        "class",
        "const",
        "continue",
        "decimal",
        "default",
        "else",
        "enum",
        "false",
        "for",
        "foreach",
        "if",
        "internal",
        "namespace",
        "new",
        "null",
        "private",
        "protected",
        "public",
        "readonly",
        "record",
        "return",
        "sealed",
        "static",
        "string",
        "switch",
        "true",
        "try",
        "using",
        "var",
        "void",
        "while"
    };

    public static IReadOnlyList<RenderedCodeLineViewModel> Highlight(string code, string language)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(language);

        string normalizedCode = code.ReplaceLineEndings("\n");
        string[] lines = normalizedCode.Split('\n');
        List<RenderedCodeLineViewModel> renderedLines = [];
        foreach (string line in lines)
        {
            renderedLines.Add(new RenderedCodeLineViewModel(HighlightLine(line, language)));
        }

        return renderedLines;
    }

    private static IReadOnlyList<RenderedCodeTokenViewModel> HighlightLine(string line, string language) =>
        IsCSharp(language)
            ? HighlightCSharpLine(line)
            : [new RenderedCodeTokenViewModel(line, RenderedCodeTokenKind.Plain)];

    private static bool IsCSharp(string language) =>
        language.Equals("csharp", StringComparison.OrdinalIgnoreCase)
        || language.Equals("cs", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<RenderedCodeTokenViewModel> HighlightCSharpLine(string line)
    {
        List<RenderedCodeTokenViewModel> tokens = [];
        int cursor = 0;
        while (cursor < line.Length)
        {
            if (IsLineCommentStart(line, cursor))
            {
                tokens.Add(new RenderedCodeTokenViewModel(line[cursor..], RenderedCodeTokenKind.Comment));
                break;
            }

            char current = line[cursor];
            if (current == '"')
            {
                int stringEnd = FindStringEnd(line, cursor);
                tokens.Add(new RenderedCodeTokenViewModel(line[cursor..stringEnd], RenderedCodeTokenKind.StringLiteral));
                cursor = stringEnd;
                continue;
            }

            if (char.IsLetter(current) || current == '_')
            {
                int identifierEnd = cursor + 1;
                while (identifierEnd < line.Length
                       && (char.IsLetterOrDigit(line[identifierEnd]) || line[identifierEnd] == '_'))
                {
                    identifierEnd++;
                }

                string identifier = line[cursor..identifierEnd];
                RenderedCodeTokenKind kind = CSharpKeywords.Contains(identifier)
                    ? RenderedCodeTokenKind.Keyword
                    : RenderedCodeTokenKind.Plain;
                tokens.Add(new RenderedCodeTokenViewModel(identifier, kind));
                cursor = identifierEnd;
                continue;
            }

            if (char.IsDigit(current))
            {
                int literalEnd = cursor + 1;
                while (literalEnd < line.Length && char.IsDigit(line[literalEnd]))
                {
                    literalEnd++;
                }

                tokens.Add(new RenderedCodeTokenViewModel(line[cursor..literalEnd], RenderedCodeTokenKind.NumberLiteral));
                cursor = literalEnd;
                continue;
            }

            int plainEnd = cursor + 1;
            while (plainEnd < line.Length
                   && !IsLineCommentStart(line, plainEnd)
                   && line[plainEnd] != '"'
                   && !char.IsLetterOrDigit(line[plainEnd])
                   && line[plainEnd] != '_')
            {
                plainEnd++;
            }

            tokens.Add(new RenderedCodeTokenViewModel(line[cursor..plainEnd], RenderedCodeTokenKind.Plain));
            cursor = plainEnd;
        }

        return tokens;
    }

    private static bool IsLineCommentStart(string line, int cursor) =>
        cursor + 1 < line.Length && line[cursor] == '/' && line[cursor + 1] == '/';

    private static int FindStringEnd(string line, int stringStart)
    {
        int cursor = stringStart + 1;
        bool isEscaped = false;
        while (cursor < line.Length)
        {
            char current = line[cursor];
            if (current == '"' && !isEscaped)
            {
                return cursor + 1;
            }

            isEscaped = current == '\\' && !isEscaped;
            if (current != '\\')
            {
                isEscaped = false;
            }

            cursor++;
        }

        return line.Length;
    }
}
