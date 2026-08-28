using Zaphira.Domain;

namespace Zaphira.Application.Providers;

public sealed record ProviderModelInstallationProgressEvent : ProviderModelInstallationEvent
{
    public ProviderModelInstallationProgressEvent(
        ModelId modelId,
        string status,
        long completedBytes,
        long totalBytes,
        bool hasKnownTotalBytes)
    {
        ArgumentNullException.ThrowIfNull(modelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        if (completedBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(completedBytes), "Completed bytes cannot be negative.");
        }

        if (totalBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalBytes), "Total bytes cannot be negative.");
        }

        ModelId = modelId;
        Status = status;
        CompletedBytes = completedBytes;
        TotalBytes = totalBytes;
        HasKnownTotalBytes = hasKnownTotalBytes;
    }

    public ModelId ModelId { get; }

    public string Status { get; }

    public long CompletedBytes { get; }

    public long TotalBytes { get; }

    public bool HasKnownTotalBytes { get; }
}
