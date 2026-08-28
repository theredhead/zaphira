using Zaphira.Client.Configuration;
using Zaphira.Client.Storage;

namespace Zaphira.Client.Logging;

public sealed class ZaphiraClientStartupLogger
{
    private readonly ZaphiraClientDataDirectories dataDirectories;

    public ZaphiraClientStartupLogger(ZaphiraClientDataDirectories dataDirectories)
    {
        ArgumentNullException.ThrowIfNull(dataDirectories);

        this.dataDirectories = dataDirectories;
    }

    public async Task LogStartupAsync(ZaphiraClientConfiguration configuration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        await dataDirectories.EnsureClientDirectoriesExistAsync(cancellationToken);

        string line =
            $"{DateTimeOffset.UtcNow:O} Zaphira client started. BackendAddress={configuration.BackendAddress} StartsInFirstRun={configuration.StartsInFirstRun}";

        await File.AppendAllLinesAsync(dataDirectories.LogFile, [line], cancellationToken);
    }
}
