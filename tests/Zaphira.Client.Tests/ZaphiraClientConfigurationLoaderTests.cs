using Zaphira.Client.Configuration;
using Zaphira.Client.Storage;

namespace Zaphira.Client.Tests;

public sealed class ZaphiraClientConfigurationLoaderTests
{
    [Fact]
    public async Task LoadOrCreateAsyncCreatesDefaultSettingsWhenMissing()
    {
        string homeDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        ZaphiraClientDataDirectories directories = ZaphiraClientDataDirectories.ForHomeDirectory(homeDirectory);
        ZaphiraClientConfigurationLoader loader = new(directories);

        ZaphiraClientConfiguration configuration = await loader.LoadOrCreateAsync(CancellationToken.None);

        Assert.Equal(new Uri("https://localhost:5051"), configuration.BackendAddress);
        Assert.True(configuration.StartsInFirstRun);
        Assert.True(File.Exists(directories.SettingsFile));

        Directory.Delete(homeDirectory, recursive: true);
    }
}
