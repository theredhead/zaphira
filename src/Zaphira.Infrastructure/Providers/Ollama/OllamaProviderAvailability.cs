namespace Zaphira.Infrastructure.Providers.Ollama;

public sealed record OllamaProviderAvailability
{
    public OllamaProviderAvailability(bool isAvailable, string version, string message, string suggestion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(suggestion);

        IsAvailable = isAvailable;
        Version = version;
        Message = message;
        Suggestion = suggestion;
    }

    public bool IsAvailable { get; }

    public string Version { get; }

    public string Message { get; }

    public string Suggestion { get; }

    public static OllamaProviderAvailability Available(string version) =>
        new(true, version, "Ollama is available.", "Continue.");

    public static OllamaProviderAvailability Unavailable() =>
        new(false, "Unavailable", "Ollama is unavailable.", "Start Ollama or go online to install it, then try again.");
}
