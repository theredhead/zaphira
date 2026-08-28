using Zaphira.Application.ModelCatalog;
using Zaphira.Infrastructure.ModelCatalog;

namespace Zaphira.Infrastructure.Tests;

public sealed class FileModelCatalogCacheTests
{
    [Fact]
    public async Task LoadAsyncReturnsNotFoundWhenCacheFileDoesNotExist()
    {
        string cacheFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "catalog.json");
        FileModelCatalogCache cache = new(cacheFile);

        CatalogCacheLookup lookup = await cache.LoadAsync(CancellationToken.None);

        Assert.False(lookup.Exists);
        Assert.Empty(lookup.Snapshot.Models);
    }

    [Fact]
    public async Task SaveAsyncPersistsCatalogSnapshot()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string cacheFile = Path.Combine(directory, "catalog.json");
        FileModelCatalogCache cache = new(cacheFile);
        CatalogSnapshot snapshot = new(
            "hugging-face",
            DateTimeOffset.UtcNow,
            [
                new CatalogModelSummary(
                    "microsoft/phi-4",
                    "phi-4",
                    ["text-generation"],
                    [CatalogModelPurpose.GeneralChat])
            ]);

        await cache.SaveAsync(snapshot, CancellationToken.None);
        CatalogCacheLookup lookup = await cache.LoadAsync(CancellationToken.None);

        Assert.True(lookup.Exists);
        Assert.Equal(snapshot.SourceId, lookup.Snapshot.SourceId);
        Assert.Equal(snapshot.FetchedAt, lookup.Snapshot.FetchedAt);
        CatalogModelSummary model = Assert.Single(lookup.Snapshot.Models);
        Assert.Equal("microsoft/phi-4", model.Id);
        Assert.Contains(CatalogModelPurpose.GeneralChat, model.Purposes);

        DeleteDirectoryIfItExists(directory);
    }

    [Fact]
    public async Task LoadAsyncReturnsNotFoundWhenCacheFileIsInvalid()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string cacheFile = Path.Combine(directory, "catalog.json");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(cacheFile, "{ invalid json", CancellationToken.None);
        FileModelCatalogCache cache = new(cacheFile);

        CatalogCacheLookup lookup = await cache.LoadAsync(CancellationToken.None);

        Assert.False(lookup.Exists);

        DeleteDirectoryIfItExists(directory);
    }

    private static void DeleteDirectoryIfItExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
