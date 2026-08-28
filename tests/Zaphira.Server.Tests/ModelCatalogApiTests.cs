using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Zaphira.Application.ModelCatalog;
using Zaphira.Contracts;

namespace Zaphira.Server.Tests;

public sealed class ModelCatalogApiTests
{
    private static readonly CatalogModelSummary CatalogModel =
        new("microsoft/phi-4", "phi-4", ["text-generation"], [CatalogModelPurpose.GeneralChat]);

    [Fact]
    public async Task GetCatalogReturnsCatalogModels()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(
            homeDirectory,
            new FakeCatalogSource(CatalogSourceResult.Available([CatalogModel])),
            new FakeModelCatalogCache(CatalogCacheLookup.NotFound()));
        using HttpClient client = factory.CreateClient();

        ModelCatalogResponse? response = await client.GetFromJsonAsync<ModelCatalogResponse>("/api/model-catalog/");

        Assert.NotNull(response);
        Assert.False(response.IsFromCache);
        CatalogModelResponse model = Assert.Single(response.Models);
        Assert.Equal("microsoft/phi-4", model.Id);
        Assert.Contains("GeneralChat", model.Purposes);
        Assert.Equal("DirectlyUsable", model.CompatibilityStatus);
        Assert.Equal("Medium", model.CompatibilityConfidence);
        Assert.Equal("Matched catalog filters.", model.MatchExplanation);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task GetCatalogFiltersByQuery()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(
            homeDirectory,
            new FakeCatalogSource(CatalogSourceResult.Available(CreateSearchModels())),
            new FakeModelCatalogCache(CatalogCacheLookup.NotFound()));
        using HttpClient client = factory.CreateClient();

        ModelCatalogResponse? response = await client.GetFromJsonAsync<ModelCatalogResponse>(
            "/api/model-catalog/?query=coder");

        Assert.NotNull(response);
        CatalogModelResponse model = Assert.Single(response.Models);
        Assert.Equal("Qwen/Qwen2.5-Coder-7B-Instruct", model.Id);
        Assert.Equal("Matched name or id: coder.", model.MatchExplanation);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task GetCatalogFiltersByPurpose()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(
            homeDirectory,
            new FakeCatalogSource(CatalogSourceResult.Available(CreateSearchModels())),
            new FakeModelCatalogCache(CatalogCacheLookup.NotFound()));
        using HttpClient client = factory.CreateClient();

        ModelCatalogResponse? response = await client.GetFromJsonAsync<ModelCatalogResponse>(
            "/api/model-catalog/?purpose=Coding");

        Assert.NotNull(response);
        CatalogModelResponse model = Assert.Single(response.Models);
        Assert.Equal("Qwen/Qwen2.5-Coder-7B-Instruct", model.Id);
        Assert.Contains("Coding", model.Purposes);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task SyncCatalogBypassesFreshCache()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();
        FakeCatalogSource source = new(CatalogSourceResult.Available([CatalogModel]));
        FakeModelCatalogCache cache = new(CatalogCacheLookup.Found(
            new CatalogSnapshot("fake", DateTimeOffset.UtcNow, [CatalogModel])));

        await using ZaphiraServerApplicationFactory factory = new(homeDirectory, source, cache);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsync("/api/model-catalog/sync", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, source.LoadCallCount);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task GetCatalogReturnsCachedCatalogWhenSourceIsUnavailable()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(
            homeDirectory,
            new FakeCatalogSource(CatalogSourceResult.Unavailable(
                "Model catalog is unavailable.",
                "Go online and try syncing the catalog again.")),
            new FakeModelCatalogCache(CatalogCacheLookup.Found(
                new CatalogSnapshot("fake", DateTimeOffset.UtcNow.AddHours(-25), [CatalogModel]))));
        using HttpClient client = factory.CreateClient();

        ModelCatalogResponse? response = await client.GetFromJsonAsync<ModelCatalogResponse>("/api/model-catalog/");

        Assert.NotNull(response);
        Assert.True(response.IsFromCache);
        Assert.Equal("Cached catalog loaded because sync is unavailable.", response.Message);
        Assert.Single(response.Models);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task GetCatalogReturnsUnavailableWhenSourceFailsWithoutCache()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(
            homeDirectory,
            new FakeCatalogSource(CatalogSourceResult.Unavailable(
                "Model catalog is unavailable.",
                "Go online and try syncing the catalog again.")),
            new FakeModelCatalogCache(CatalogCacheLookup.NotFound()));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/model-catalog/");
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("catalog_unavailable", error.Code);
        Assert.Equal("Go online and try syncing the catalog again.", error.Suggestion);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    private static string CreateTemporaryHomeDirectory() =>
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    private static IReadOnlyList<CatalogModelSummary> CreateSearchModels() =>
    [
        CatalogModel,
        new CatalogModelSummary(
            "Qwen/Qwen2.5-Coder-7B-Instruct",
            "Qwen2.5-Coder-7B-Instruct",
            ["code"],
            [CatalogModelPurpose.Coding])
    ];

    private static void DeleteDirectoryIfItExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private sealed class ZaphiraServerApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string homeDirectory;
        private readonly ICatalogSource catalogSource;
        private readonly IModelCatalogCache modelCatalogCache;

        public ZaphiraServerApplicationFactory(
            string homeDirectory,
            ICatalogSource catalogSource,
            IModelCatalogCache modelCatalogCache)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(homeDirectory);
            ArgumentNullException.ThrowIfNull(catalogSource);
            ArgumentNullException.ThrowIfNull(modelCatalogCache);

            this.homeDirectory = homeDirectory;
            this.catalogSource = catalogSource;
            this.modelCatalogCache = modelCatalogCache;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration(configurationBuilder =>
            {
                configurationBuilder.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Zaphira:HomeDirectory"] = homeDirectory
                    });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICatalogSource>();
                services.RemoveAll<IModelCatalogCache>();
                services.RemoveAll<ModelCatalogService>();
                services.AddSingleton(catalogSource);
                services.AddSingleton(modelCatalogCache);
                services.AddSingleton(serviceProvider =>
                    new ModelCatalogService(
                        serviceProvider.GetRequiredService<ICatalogSource>(),
                        serviceProvider.GetRequiredService<IModelCatalogCache>(),
                        TimeProvider.System));
            });
        }
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

    private sealed class FakeModelCatalogCache : IModelCatalogCache
    {
        private CatalogCacheLookup lookup;

        public FakeModelCatalogCache(CatalogCacheLookup lookup)
        {
            ArgumentNullException.ThrowIfNull(lookup);

            this.lookup = lookup;
        }

        public Task<CatalogCacheLookup> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(lookup);
        }

        public Task SaveAsync(CatalogSnapshot snapshot, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            cancellationToken.ThrowIfCancellationRequested();

            lookup = CatalogCacheLookup.Found(snapshot);

            return Task.CompletedTask;
        }
    }
}
