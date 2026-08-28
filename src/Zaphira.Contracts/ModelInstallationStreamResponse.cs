using System.Text.Json.Serialization;

namespace Zaphira.Contracts;

public sealed record ModelInstallationStreamResponse
{
    [JsonConstructor]
    public ModelInstallationStreamResponse(
        string kind,
        string modelId,
        string status,
        long completedBytes,
        long totalBytes,
        bool hasKnownTotalBytes,
        ErrorResponse error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        ArgumentNullException.ThrowIfNull(error);

        if (completedBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(completedBytes), "Completed bytes cannot be negative.");
        }

        if (totalBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalBytes), "Total bytes cannot be negative.");
        }

        Kind = kind;
        ModelId = modelId;
        Status = status;
        CompletedBytes = completedBytes;
        TotalBytes = totalBytes;
        HasKnownTotalBytes = hasKnownTotalBytes;
        Error = error;
    }

    public string Kind { get; }

    public string ModelId { get; }

    public string Status { get; }

    public long CompletedBytes { get; }

    public long TotalBytes { get; }

    public bool HasKnownTotalBytes { get; }

    public ErrorResponse Error { get; }

    public static ModelInstallationStreamResponse Progress(
        string modelId,
        string status,
        long completedBytes,
        long totalBytes,
        bool hasKnownTotalBytes) =>
        new(
            "progress",
            modelId,
            status,
            completedBytes,
            totalBytes,
            hasKnownTotalBytes,
            ErrorResponse.None());

    public static ModelInstallationStreamResponse Completed(string modelId) =>
        new("completed", modelId, "Completed", 0, 0, false, ErrorResponse.None());

    public static ModelInstallationStreamResponse Failed(string modelId, ErrorResponse error) =>
        new("failed", modelId, "Failed", 0, 0, false, error);
}
