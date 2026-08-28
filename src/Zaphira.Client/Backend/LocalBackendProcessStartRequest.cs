namespace Zaphira.Client.Backend;

public sealed record LocalBackendProcessStartRequest
{
    public LocalBackendProcessStartRequest(string executablePath, string arguments, string workingDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);

        ExecutablePath = executablePath;
        Arguments = arguments;
        WorkingDirectory = workingDirectory;
    }

    public string ExecutablePath { get; }

    public string Arguments { get; }

    public string WorkingDirectory { get; }
}
