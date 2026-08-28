namespace Zaphira.Domain;

public sealed record FileReference
{
    public FileReference(string displayName, string mediaType, string storageLocation, long sizeInBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);
        ArgumentException.ThrowIfNullOrWhiteSpace(storageLocation);
        ArgumentOutOfRangeException.ThrowIfNegative(sizeInBytes);

        DisplayName = displayName;
        MediaType = mediaType;
        StorageLocation = storageLocation;
        SizeInBytes = sizeInBytes;
    }

    public string DisplayName { get; }

    public string MediaType { get; }

    public string StorageLocation { get; }

    public long SizeInBytes { get; }
}
