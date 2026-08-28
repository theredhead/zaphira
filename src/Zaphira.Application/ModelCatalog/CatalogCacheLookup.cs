namespace Zaphira.Application.ModelCatalog;

public sealed record CatalogCacheLookup
{
    private CatalogCacheLookup(bool exists, CatalogSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        Exists = exists;
        Snapshot = snapshot;
    }

    public bool Exists { get; }

    public CatalogSnapshot Snapshot { get; }

    public static CatalogCacheLookup Found(CatalogSnapshot snapshot) => new(exists: true, snapshot);

    public static CatalogCacheLookup NotFound() =>
        new(
            exists: false,
            new CatalogSnapshot(
                "none",
                DateTimeOffset.UnixEpoch,
                []));
}
