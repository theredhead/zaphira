using Zaphira.Client.Security;
using Zaphira.Client.Storage;

namespace Zaphira.Client.Tests;

public sealed class TrustedBackendConnectionStoreTests
{
    [Fact]
    public async Task SaveAsyncPersistsTrustedConnection()
    {
        string homeDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        ZaphiraClientDataDirectories directories = ZaphiraClientDataDirectories.ForHomeDirectory(homeDirectory);
        TrustedBackendConnectionStore firstStore = new(directories);
        TrustedBackendConnection connection = new(
            new Uri("https://localhost:5051"),
            "ABCDEF123456",
            "Local backend certificate.");

        await firstStore.SaveAsync(connection, CancellationToken.None);

        TrustedBackendConnectionStore secondStore = new(directories);
        IReadOnlyList<TrustedBackendConnection> connections = await secondStore.LoadAsync(CancellationToken.None);

        Assert.Equal([connection], connections);

        Directory.Delete(homeDirectory, recursive: true);
    }

    [Fact]
    public async Task LoadAsyncReturnsEmptyListWhenConnectionsFileIsMissing()
    {
        string homeDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        ZaphiraClientDataDirectories directories = ZaphiraClientDataDirectories.ForHomeDirectory(homeDirectory);
        TrustedBackendConnectionStore store = new(directories);

        IReadOnlyList<TrustedBackendConnection> connections = await store.LoadAsync(CancellationToken.None);

        Assert.Empty(connections);
    }
}
