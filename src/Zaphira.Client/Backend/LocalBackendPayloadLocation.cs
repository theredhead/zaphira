namespace Zaphira.Client.Backend;

public abstract record LocalBackendPayloadLocation;

public sealed record AvailableLocalBackendPayload : LocalBackendPayloadLocation
{
    public AvailableLocalBackendPayload(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        ExecutablePath = Path.GetFullPath(executablePath);
    }

    public string ExecutablePath { get; }
}

public sealed record MissingLocalBackendPayload : LocalBackendPayloadLocation
{
    public MissingLocalBackendPayload(string searchedDirectory, string suggestion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchedDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(suggestion);

        SearchedDirectory = Path.GetFullPath(searchedDirectory);
        Suggestion = suggestion;
    }

    public string SearchedDirectory { get; }

    public string Suggestion { get; }
}
