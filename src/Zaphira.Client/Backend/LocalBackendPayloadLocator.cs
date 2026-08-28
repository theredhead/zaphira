namespace Zaphira.Client.Backend;

public sealed class LocalBackendPayloadLocator
{
    private const string MissingPayloadSuggestion = "Install or build the Zaphira server payload, then try again.";

    private readonly string searchDirectory;

    public LocalBackendPayloadLocator(string searchDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchDirectory);

        this.searchDirectory = Path.GetFullPath(searchDirectory);
    }

    public LocalBackendPayloadLocation Locate()
    {
        string[] candidateFileNames =
        [
            "Zaphira.Server",
            "Zaphira.Server.exe",
            "Zaphira.Server.dll"
        ];

        foreach (string candidateFileName in candidateFileNames)
        {
            string candidatePath = Path.Combine(searchDirectory, candidateFileName);
            if (File.Exists(candidatePath))
            {
                return new AvailableLocalBackendPayload(candidatePath);
            }
        }

        return new MissingLocalBackendPayload(searchDirectory, MissingPayloadSuggestion);
    }
}
