using System.Text.Json;
using Zaphira.Client.Storage;

namespace Zaphira.Client.Configuration;

public sealed class ZaphiraClientConfigurationLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly ZaphiraClientDataDirectories dataDirectories;

    public ZaphiraClientConfigurationLoader(ZaphiraClientDataDirectories dataDirectories)
    {
        ArgumentNullException.ThrowIfNull(dataDirectories);

        this.dataDirectories = dataDirectories;
    }

    public async Task<ZaphiraClientConfiguration> LoadOrCreateAsync(CancellationToken cancellationToken)
    {
        await dataDirectories.EnsureClientDirectoriesExistAsync(cancellationToken);

        if (!File.Exists(dataDirectories.SettingsFile))
        {
            ZaphiraClientConfiguration defaultConfiguration = ZaphiraClientConfiguration.Default();
            await SaveAsync(defaultConfiguration, cancellationToken);

            return defaultConfiguration;
        }

        await using FileStream stream = File.OpenRead(dataDirectories.SettingsFile);
        ClientConfigurationFile? configurationFile =
            await JsonSerializer.DeserializeAsync<ClientConfigurationFile>(stream, SerializerOptions, cancellationToken);

        if (configurationFile is null)
        {
            throw new InvalidOperationException("Client settings file did not contain configuration.");
        }

        return configurationFile.ToConfiguration();
    }

    private async Task SaveAsync(ZaphiraClientConfiguration configuration, CancellationToken cancellationToken)
    {
        ClientConfigurationFile configurationFile = ClientConfigurationFile.FromConfiguration(configuration);
        await using FileStream stream = File.Create(dataDirectories.SettingsFile);

        await JsonSerializer.SerializeAsync(stream, configurationFile, SerializerOptions, cancellationToken);
    }

    private sealed record ClientConfigurationFile
    {
        public ClientConfigurationFile(string backendAddress, bool startsInFirstRun)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(backendAddress);

            BackendAddress = backendAddress;
            StartsInFirstRun = startsInFirstRun;
        }

        public string BackendAddress { get; }

        public bool StartsInFirstRun { get; }

        public static ClientConfigurationFile FromConfiguration(ZaphiraClientConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            return new ClientConfigurationFile(configuration.BackendAddress.ToString(), configuration.StartsInFirstRun);
        }

        public ZaphiraClientConfiguration ToConfiguration() =>
            new(new Uri(BackendAddress), StartsInFirstRun);
    }
}
