using System.Diagnostics;

namespace Zaphira.Client.Backend;

public sealed class OperatingSystemBackendProcessLauncher : IBackendProcessLauncher
{
    public Task<IBackendProcess> StartAsync(LocalBackendProcessStartRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        ProcessStartInfo startInfo = CreateStartInfo(request);
        Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Operating system did not return a backend process.");

        return Task.FromResult<IBackendProcess>(new OperatingSystemBackendProcess(process));
    }

    private static ProcessStartInfo CreateStartInfo(LocalBackendProcessStartRequest request)
    {
        if (Path.GetExtension(request.ExecutablePath).Equals(".dll", StringComparison.OrdinalIgnoreCase))
        {
            return new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"{Quote(request.ExecutablePath)} {request.Arguments}".Trim(),
                WorkingDirectory = request.WorkingDirectory,
                UseShellExecute = false
            };
        }

        return new ProcessStartInfo
        {
            FileName = request.ExecutablePath,
            Arguments = request.Arguments,
            WorkingDirectory = request.WorkingDirectory,
            UseShellExecute = false
        };
    }

    private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";

    private sealed class OperatingSystemBackendProcess : IBackendProcess
    {
        private readonly Process process;

        public OperatingSystemBackendProcess(Process process)
        {
            ArgumentNullException.ThrowIfNull(process);

            this.process = process;
        }

        public int ProcessId => process.Id;

        public bool HasExited => process.HasExited;

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (process.HasExited)
            {
                return;
            }

            process.CloseMainWindow();
            await process.WaitForExitAsync(cancellationToken);
        }
    }
}
