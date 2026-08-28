namespace Zaphira.Client.Backend;

public sealed record BackendProcessStatus
{
    public BackendProcessStatus(BackendProcessState state, BackendOwnership ownership, int processId, string message)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(processId);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        State = state;
        Ownership = ownership;
        ProcessId = processId;
        Message = message;
    }

    public BackendProcessState State { get; }

    public BackendOwnership Ownership { get; }

    public int ProcessId { get; }

    public string Message { get; }

    public static BackendProcessStatus RunningOwned(int processId) =>
        new(BackendProcessState.Running, BackendOwnership.OwnedByClient, processId, "Backend is running.");

    public static BackendProcessStatus RunningExternal(int processId) =>
        new(BackendProcessState.Running, BackendOwnership.External, processId, "External backend is running.");

    public static BackendProcessStatus BackendAlreadyAvailable() =>
        new(BackendProcessState.Running, BackendOwnership.External, 0, "Backend is already reachable.");

    public static BackendProcessStatus LocalBackendNotRequired() =>
        new(BackendProcessState.NotStarted, BackendOwnership.None, 0, "Local backend is not required.");

    public static BackendProcessStatus Failed(string message) =>
        new(BackendProcessState.Failed, BackendOwnership.None, 0, message);

    public static BackendProcessStatus Stopped() =>
        new(BackendProcessState.Stopped, BackendOwnership.None, 0, "Backend is stopped.");
}
