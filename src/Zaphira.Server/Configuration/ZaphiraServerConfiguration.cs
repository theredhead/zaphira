using Zaphira.Infrastructure.Storage;

namespace Zaphira.Server.Configuration;

internal static class ZaphiraServerConfiguration
{
    private const string HomeDirectoryConfigurationKey = "Zaphira:HomeDirectory";

    public static ZaphiraDataDirectories LoadDataDirectories(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string configuredHomeDirectory = configuration[HomeDirectoryConfigurationKey] ?? string.Empty;

        return string.IsNullOrWhiteSpace(configuredHomeDirectory)
            ? ZaphiraDataDirectories.ForCurrentUser()
            : ZaphiraDataDirectories.ForHomeDirectory(configuredHomeDirectory);
    }
}
