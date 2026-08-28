using Zaphira.Application.ModelCatalog;

namespace Zaphira.Application.Tests;

public sealed class ModelCatalogServiceTests
{
    private static readonly CatalogModelSummary CatalogModel =
        new("microsoft/phi-4", "phi-4", ["text-generation"], [CatalogModelPurpose.GeneralChat]);

    [Fact]
    public async Task LoadAsyncReturnsFreshCachedCatalogWithoutSyncing()
    {
        FixedTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        FakeCatalogCache cache = new(CatalogCacheLookup.Found(
            new CatalogSnapshot("hugging-face", timeProvider.GetUtcNow(), [CatalogModel])));
        FakeCatalogSource source = new(CatalogSourceResult.Unavailable(
            "Model catalog is unavailable.",
            "Go online and try syncing the catalog again."));
        ModelCatalogService service = new(source, cache, timeProvider);

        ModelCatalogLoadResult result = await service.LoadAsync(forceSync: false, CancellationToken.None);

        Assert.True(result.IsAvailable);
        Assert.True(result.IsFromCache);
        Assert.Equal(0, source.LoadCallCount);
        Assert.Single(result.Models);
    }

    [Fact]
    public async Task LoadAsyncRefreshesStaleCachedCatalog()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        FixedTimeProvider timeProvider = new(now);
        FakeCatalogCache cache = new(CatalogCacheLookup.Found(
            new CatalogSnapshot("hugging-face", now.AddHours(-25), [CatalogModel])));
        CatalogModelSummary refreshedModel = new(
            "Qwen/Qwen2.5-Coder-7B-Instruct",
            "Qwen2.5-Coder-7B-Instruct",
            ["code"],
            [CatalogModelPurpose.Coding]);
        FakeCatalogSource source = new(CatalogSourceResult.Available([refreshedModel]));
        ModelCatalogService service = new(source, cache, timeProvider);

        ModelCatalogLoadResult result = await service.LoadAsync(forceSync: false, CancellationToken.None);

        Assert.True(result.IsAvailable);
        Assert.False(result.IsFromCache);
        Assert.Equal(1, source.LoadCallCount);
        Assert.Equal(refreshedModel.Id, Assert.Single(result.Models).Id);
        Assert.True(cache.SaveCallCount > 0);
    }

    [Fact]
    public async Task LoadAsyncReturnsCachedCatalogWhenSyncIsUnavailable()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        FixedTimeProvider timeProvider = new(now);
        FakeCatalogCache cache = new(CatalogCacheLookup.Found(
            new CatalogSnapshot("hugging-face", now.AddHours(-25), [CatalogModel])));
        FakeCatalogSource source = new(CatalogSourceResult.Unavailable(
            "Model catalog is unavailable.",
            "Go online and try syncing the catalog again."));
        ModelCatalogService service = new(source, cache, timeProvider);

        ModelCatalogLoadResult result = await service.LoadAsync(forceSync: false, CancellationToken.None);

        Assert.True(result.IsAvailable);
        Assert.True(result.IsFromCache);
        Assert.Equal("Cached catalog loaded because sync is unavailable.", result.Message);
        Assert.Single(result.Models);
    }

    [Fact]
    public async Task LoadAsyncReturnsUnavailableWhenSyncFailsWithoutCache()
    {
        FixedTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
        FakeCatalogCache cache = new(CatalogCacheLookup.NotFound());
        FakeCatalogSource source = new(CatalogSourceResult.Unavailable(
            "Model catalog is unavailable.",
            "Go online and try syncing the catalog again."));
        ModelCatalogService service = new(source, cache, timeProvider);

        ModelCatalogLoadResult result = await service.LoadAsync(forceSync: false, CancellationToken.None);

        Assert.False(result.IsAvailable);
        Assert.False(result.IsFromCache);
        Assert.Equal("Model catalog is unavailable.", result.Message);
        Assert.Equal("Go online and try syncing the catalog again.", result.Suggestion);
    }

    private sealed class FakeCatalogSource : ICatalogSource
    {
        private readonly CatalogSourceResult result;

        public FakeCatalogSource(CatalogSourceResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            this.result = result;
        }

        public string Id { get; } = "fake";

        public string DisplayName { get; } = "Fake Catalog";

        public int LoadCallCount { get; private set; }

        public Task<CatalogSourceResult> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LoadCallCount++;

            return Task.FromResult(result);
        }
    }

    private sealed class FakeCatalogCache : IModelCatalogCache
    {
        private CatalogCacheLookup lookup;

        public FakeCatalogCache(CatalogCacheLookup lookup)
        {
            ArgumentNullException.ThrowIfNull(lookup);

            this.lookup = lookup;
        }

        public int SaveCallCount { get; private set; }

        public Task<CatalogCacheLookup> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(lookup);
        }

        public Task SaveAsync(CatalogSnapshot snapshot, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            cancellationToken.ThrowIfCancellationRequested();

            SaveCallCount++;
            lookup = CatalogCacheLookup.Found(snapshot);

            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            this.utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
