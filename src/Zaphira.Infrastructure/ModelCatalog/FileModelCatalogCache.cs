using System.Text.Json;
using Zaphira.Application.ModelCatalog;

namespace Zaphira.Infrastructure.ModelCatalog;

public sealed class FileModelCatalogCache : IModelCatalogCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string cacheFile;

    public FileModelCatalogCache(string cacheFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheFile);

        this.cacheFile = cacheFile;
    }

    public async Task<CatalogCacheLookup> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(cacheFile))
        {
            return CatalogCacheLookup.NotFound();
        }

        try
        {
            await using FileStream stream = File.OpenRead(cacheFile);
            CatalogCacheFile? file = await JsonSerializer.DeserializeAsync<CatalogCacheFile>(
                stream,
                SerializerOptions,
                cancellationToken);

            return file is null
                ? CatalogCacheLookup.NotFound()
                : CatalogCacheLookup.Found(file.ToSnapshot());
        }
        catch (Exception exception) when (exception is IOException
                                        || exception is UnauthorizedAccessException
                                        || exception is JsonException)
        {
            return CatalogCacheLookup.NotFound();
        }
    }

    public async Task SaveAsync(CatalogSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        string directory = Path.GetDirectoryName(cacheFile) ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using FileStream stream = File.Create(cacheFile);
        await JsonSerializer.SerializeAsync(
            stream,
            CatalogCacheFile.FromSnapshot(snapshot),
            SerializerOptions,
            cancellationToken);
    }

    private sealed record CatalogCacheFile
    {
        public CatalogCacheFile(string sourceId, DateTimeOffset fetchedAt, IReadOnlyList<CatalogModelFile> models)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
            ArgumentNullException.ThrowIfNull(models);

            CatalogModelFile[] materializedModels = models.ToArray();
            if (materializedModels.Any(model => model is null))
            {
                throw new ArgumentException("Cached catalog models cannot contain null values.", nameof(models));
            }

            SourceId = sourceId;
            FetchedAt = fetchedAt;
            Models = materializedModels;
        }

        public string SourceId { get; }

        public DateTimeOffset FetchedAt { get; }

        public IReadOnlyList<CatalogModelFile> Models { get; }

        public CatalogSnapshot ToSnapshot() =>
            new(SourceId, FetchedAt, Models.Select(model => model.ToSummary()).ToArray());

        public static CatalogCacheFile FromSnapshot(CatalogSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            return new CatalogCacheFile(
                snapshot.SourceId,
                snapshot.FetchedAt,
                snapshot.Models.Select(CatalogModelFile.FromSummary).ToArray());
        }
    }

    private sealed record CatalogModelFile
    {
        public CatalogModelFile(
            string id,
            string displayName,
            IReadOnlyList<string> tags,
            IReadOnlyList<CatalogModelPurpose> purposes)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
            ArgumentNullException.ThrowIfNull(tags);
            ArgumentNullException.ThrowIfNull(purposes);

            string[] materializedTags = tags.ToArray();
            if (materializedTags.Any(tag => tag is null))
            {
                throw new ArgumentException("Cached model tags cannot contain null values.", nameof(tags));
            }

            Id = id;
            DisplayName = displayName;
            Tags = materializedTags;
            Purposes = purposes.ToArray();
        }

        public string Id { get; }

        public string DisplayName { get; }

        public IReadOnlyList<string> Tags { get; }

        public IReadOnlyList<CatalogModelPurpose> Purposes { get; }

        public CatalogModelSummary ToSummary() =>
            new(Id, DisplayName, Tags, Purposes);

        public static CatalogModelFile FromSummary(CatalogModelSummary summary)
        {
            ArgumentNullException.ThrowIfNull(summary);

            return new CatalogModelFile(summary.Id, summary.DisplayName, summary.Tags, summary.Purposes);
        }
    }
}
