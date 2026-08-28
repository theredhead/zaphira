namespace Zaphira.Infrastructure.Storage;

public sealed record ZaphiraDataDirectories
{
    private ZaphiraDataDirectories(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);

        Root = Path.GetFullPath(root);
        ClientRoot = Path.Combine(Root, "client");
        ServerRoot = Path.Combine(Root, "server");
        ServerDatabaseFile = Path.Combine(ServerRoot, "server.db");
        ServerSettingsFile = Path.Combine(ServerRoot, "settings.json");
        ServerCertificatesRoot = Path.Combine(ServerRoot, "certificates");
        ServerPairingsRoot = Path.Combine(ServerRoot, "pairings");
        ServerCacheRoot = Path.Combine(ServerRoot, "cache");
        ServerFilesRoot = Path.Combine(ServerRoot, "files");
        ServerAttachmentsRoot = Path.Combine(ServerFilesRoot, "attachments");
        ServerAudioRoot = Path.Combine(ServerFilesRoot, "audio");
        ServerLogsRoot = Path.Combine(ServerRoot, "logs");
    }

    public string Root { get; }

    public string ClientRoot { get; }

    public string ServerRoot { get; }

    public string ServerDatabaseFile { get; }

    public string ServerSettingsFile { get; }

    public string ServerCertificatesRoot { get; }

    public string ServerPairingsRoot { get; }

    public string ServerCacheRoot { get; }

    public string ServerFilesRoot { get; }

    public string ServerAttachmentsRoot { get; }

    public string ServerAudioRoot { get; }

    public string ServerLogsRoot { get; }

    public static ZaphiraDataDirectories ForHomeDirectory(string homeDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(homeDirectory);

        return new ZaphiraDataDirectories(Path.Combine(homeDirectory, ".zaphira"));
    }

    public static ZaphiraDataDirectories ForCurrentUser()
    {
        string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return ForHomeDirectory(homeDirectory);
    }

    public async Task EnsureServerDirectoriesExistAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string[] directories =
        [
            ServerRoot,
            ServerCertificatesRoot,
            ServerPairingsRoot,
            ServerCacheRoot,
            ServerFilesRoot,
            ServerAttachmentsRoot,
            ServerAudioRoot,
            ServerLogsRoot
        ];

        foreach (string directory in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(directory);
        }

        await Task.CompletedTask;
    }
}
