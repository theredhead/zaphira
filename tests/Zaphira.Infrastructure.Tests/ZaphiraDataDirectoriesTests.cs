using Zaphira.Infrastructure.Storage;

namespace Zaphira.Infrastructure.Tests;

public sealed class ZaphiraDataDirectoriesTests
{
    [Fact]
    public void ForHomeDirectoryBuildsExpectedLayout()
    {
        string homeDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        ZaphiraDataDirectories directories = ZaphiraDataDirectories.ForHomeDirectory(homeDirectory);

        Assert.Equal(Path.Combine(homeDirectory, ".zaphira"), directories.Root);
        Assert.Equal(Path.Combine(homeDirectory, ".zaphira", "client"), directories.ClientRoot);
        Assert.Equal(Path.Combine(homeDirectory, ".zaphira", "server"), directories.ServerRoot);
        Assert.Equal(Path.Combine(homeDirectory, ".zaphira", "server", "server.db"), directories.ServerDatabaseFile);
        Assert.Equal(Path.Combine(homeDirectory, ".zaphira", "server", "settings.json"), directories.ServerSettingsFile);
        Assert.Equal(Path.Combine(homeDirectory, ".zaphira", "server", "certificates"), directories.ServerCertificatesRoot);
        Assert.Equal(Path.Combine(homeDirectory, ".zaphira", "server", "pairings"), directories.ServerPairingsRoot);
        Assert.Equal(Path.Combine(homeDirectory, ".zaphira", "server", "cache"), directories.ServerCacheRoot);
        Assert.Equal(Path.Combine(homeDirectory, ".zaphira", "server", "files"), directories.ServerFilesRoot);
        Assert.Equal(Path.Combine(homeDirectory, ".zaphira", "server", "logs"), directories.ServerLogsRoot);
    }

    [Fact]
    public async Task EnsureServerDirectoriesExistAsyncCreatesServerDirectories()
    {
        string homeDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        ZaphiraDataDirectories directories = ZaphiraDataDirectories.ForHomeDirectory(homeDirectory);

        await directories.EnsureServerDirectoriesExistAsync(CancellationToken.None);

        Assert.True(Directory.Exists(directories.ServerRoot));
        Assert.True(Directory.Exists(directories.ServerCertificatesRoot));
        Assert.True(Directory.Exists(directories.ServerPairingsRoot));
        Assert.True(Directory.Exists(directories.ServerCacheRoot));
        Assert.True(Directory.Exists(directories.ServerFilesRoot));
        Assert.True(Directory.Exists(directories.ServerAttachmentsRoot));
        Assert.True(Directory.Exists(directories.ServerAudioRoot));
        Assert.True(Directory.Exists(directories.ServerLogsRoot));

        Directory.Delete(homeDirectory, recursive: true);
    }
}
