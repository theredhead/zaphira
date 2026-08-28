using Zaphira.Client.Configuration;
using Zaphira.Client.Logging;
using Zaphira.Client.Storage;

namespace Zaphira.Client.Tests;

public sealed class ZaphiraClientStartupLoggerTests
{
    [Fact]
    public async Task LogStartupAsyncWritesClientDiagnosticLog()
    {
        string homeDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        ZaphiraClientDataDirectories directories = ZaphiraClientDataDirectories.ForHomeDirectory(homeDirectory);
        ZaphiraClientStartupLogger logger = new(directories);

        await logger.LogStartupAsync(ZaphiraClientConfiguration.Default(), CancellationToken.None);

        string log = await File.ReadAllTextAsync(directories.LogFile);

        Assert.Contains("Zaphira client started.", log);
        Assert.Contains("BackendAddress=https://localhost:5051/", log);

        Directory.Delete(homeDirectory, recursive: true);
    }
}
