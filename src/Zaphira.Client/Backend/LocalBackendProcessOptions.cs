namespace Zaphira.Client.Backend;

public sealed record LocalBackendProcessOptions
{
    public LocalBackendProcessOptions(
        string executablePath,
        string arguments,
        string workingDirectory,
        int startupRetryCount,
        TimeSpan startupRetryDelay)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
        ArgumentOutOfRangeException.ThrowIfNegative(startupRetryCount);

        if (startupRetryDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(startupRetryDelay), "Startup retry delay cannot be negative.");
        }

        ExecutablePath = executablePath;
        Arguments = arguments;
        WorkingDirectory = workingDirectory;
        StartupRetryCount = startupRetryCount;
        StartupRetryDelay = startupRetryDelay;
    }

    public string ExecutablePath { get; }

    public string Arguments { get; }

    public string WorkingDirectory { get; }

    public int StartupRetryCount { get; }

    public TimeSpan StartupRetryDelay { get; }
}
