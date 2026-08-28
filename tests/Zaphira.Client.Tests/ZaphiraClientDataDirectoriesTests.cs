using Zaphira.Client.Storage;

namespace Zaphira.Client.Tests;

public sealed class ZaphiraClientDataDirectoriesTests
{
    [Fact]
    public void ForHomeDirectoryBuildsExpectedLayout()
    {
        string homeDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        ZaphiraClientDataDirectories directories = ZaphiraClientDataDirectories.ForHomeDirectory(homeDirectory);

        Assert.Equal(Path.Combine(homeDirectory, ".zaphira"), directories.Root);
        Assert.Equal(Path.Combine(homeDirectory, ".zaphira", "client"), directories.ClientRoot);
        Assert.Equal(Path.Combine(homeDirectory, ".zaphira", "client", "settings.json"), directories.SettingsFile);
        Assert.Equal(Path.Combine(homeDirectory, ".zaphira", "client", "connections.json"), directories.ConnectionsFile);
        Assert.Equal(Path.Combine(homeDirectory, ".zaphira", "client", "cache"), directories.CacheRoot);
        Assert.Equal(Path.Combine(homeDirectory, ".zaphira", "client", "logs"), directories.LogsRoot);
        Assert.Equal(Path.Combine(homeDirectory, ".zaphira", "client", "logs", "client.log"), directories.LogFile);
    }

    [Fact]
    public async Task EnsureClientDirectoriesExistAsyncCreatesClientDirectories()
    {
        string homeDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        ZaphiraClientDataDirectories directories = ZaphiraClientDataDirectories.ForHomeDirectory(homeDirectory);

        await directories.EnsureClientDirectoriesExistAsync(CancellationToken.None);

        Assert.True(Directory.Exists(directories.ClientRoot));
        Assert.True(Directory.Exists(directories.CacheRoot));
        Assert.True(Directory.Exists(directories.LogsRoot));

        Directory.Delete(homeDirectory, recursive: true);
    }
}
