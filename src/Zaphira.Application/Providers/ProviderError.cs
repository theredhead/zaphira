namespace Zaphira.Application.Providers;

public sealed record ProviderError
{
    public ProviderError(string code, string message, string suggestion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(suggestion);

        Code = code;
        Message = message;
        Suggestion = suggestion;
    }

    public string Code { get; }

    public string Message { get; }

    public string Suggestion { get; }
}
