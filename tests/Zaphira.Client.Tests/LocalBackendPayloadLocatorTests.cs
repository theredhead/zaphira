using Zaphira.Client.Backend;

namespace Zaphira.Client.Tests;

public sealed class LocalBackendPayloadLocatorTests
{
    [Fact]
    public void LocateFindsServerExecutableNextToClient()
    {
        string directory = Directory.CreateTempSubdirectory().FullName;
        string executablePath = Path.Combine(directory, "Zaphira.Server");
        File.WriteAllText(executablePath, string.Empty);
        LocalBackendPayloadLocator locator = new(directory);

        LocalBackendPayloadLocation location = locator.Locate();

        AvailableLocalBackendPayload available = Assert.IsType<AvailableLocalBackendPayload>(location);
        Assert.Equal(executablePath, available.ExecutablePath);

        Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public void LocateReturnsMissingPayloadWhenServerExecutableCannotBeFound()
    {
        string directory = Directory.CreateTempSubdirectory().FullName;
        LocalBackendPayloadLocator locator = new(directory);

        LocalBackendPayloadLocation location = locator.Locate();

        MissingLocalBackendPayload missing = Assert.IsType<MissingLocalBackendPayload>(location);
        Assert.Equal(directory, missing.SearchedDirectory);
        Assert.Equal("Install or build the Zaphira server payload, then try again.", missing.Suggestion);

        Directory.Delete(directory, recursive: true);
    }
}
