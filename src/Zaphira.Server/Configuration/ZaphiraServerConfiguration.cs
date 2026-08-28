using Zaphira.Infrastructure.Storage;

namespace Zaphira.Server.Configuration;

internal static class ZaphiraServerConfiguration
{
    private const string HomeDirectoryConfigurationKey = "Zaphira:HomeDirectory";
    private const string HttpsPortConfigurationKey = "Zaphira:Https:Port";
    private const int DefaultHttpsPort = 5051;

    public static ZaphiraDataDirectories LoadDataDirectories(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string configuredHomeDirectory = configuration[HomeDirectoryConfigurationKey] ?? string.Empty;

        return string.IsNullOrWhiteSpace(configuredHomeDirectory)
            ? ZaphiraDataDirectories.ForCurrentUser()
            : ZaphiraDataDirectories.ForHomeDirectory(configuredHomeDirectory);
    }

    public static int LoadHttpsPort(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        int configuredPort = configuration.GetValue(HttpsPortConfigurationKey, DefaultHttpsPort);

        return configuredPort <= 0
            ? throw new InvalidOperationException("Zaphira HTTPS port must be greater than zero.")
            : configuredPort;
    }
}
