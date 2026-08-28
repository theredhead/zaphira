namespace Zaphira.Application.ModelCatalog;

public sealed record CatalogSearchRequest
{
    public CatalogSearchRequest(string query, IReadOnlyList<CatalogModelPurpose> purposes)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(purposes);

        Query = query.Trim();
        Purposes = purposes.Distinct().ToArray();
    }

    public string Query { get; }

    public IReadOnlyList<CatalogModelPurpose> Purposes { get; }

    public static CatalogSearchRequest All() => new(string.Empty, []);
}
