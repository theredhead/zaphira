namespace Zaphira.Client.Storage;

public sealed record ZaphiraClientDataDirectories
{
    private ZaphiraClientDataDirectories(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);

        Root = Path.GetFullPath(root);
        ClientRoot = Path.Combine(Root, "client");
        SettingsFile = Path.Combine(ClientRoot, "settings.json");
        ConnectionsFile = Path.Combine(ClientRoot, "connections.json");
        CacheRoot = Path.Combine(ClientRoot, "cache");
        LogsRoot = Path.Combine(ClientRoot, "logs");
        LogFile = Path.Combine(LogsRoot, "client.log");
    }

    public string Root { get; }

    public string ClientRoot { get; }

    public string SettingsFile { get; }

    public string ConnectionsFile { get; }

    public string CacheRoot { get; }

    public string LogsRoot { get; }

    public string LogFile { get; }

    public static ZaphiraClientDataDirectories ForHomeDirectory(string homeDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(homeDirectory);

        return new ZaphiraClientDataDirectories(Path.Combine(homeDirectory, ".zaphira"));
    }

    public static ZaphiraClientDataDirectories ForCurrentUser()
    {
        string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return ForHomeDirectory(homeDirectory);
    }

    public async Task EnsureClientDirectoriesExistAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string[] directories =
        [
            ClientRoot,
            CacheRoot,
            LogsRoot
        ];

        foreach (string directory in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(directory);
        }

        await Task.CompletedTask;
    }
}
