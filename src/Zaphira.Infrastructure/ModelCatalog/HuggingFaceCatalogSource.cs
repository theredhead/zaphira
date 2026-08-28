using System.Text.Json;
using Zaphira.Application.ModelCatalog;

namespace Zaphira.Infrastructure.ModelCatalog;

public sealed class HuggingFaceCatalogSource : ICatalogSource
{
    private readonly HttpClient httpClient;

    public HuggingFaceCatalogSource(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        this.httpClient = httpClient;
    }

    public string Id { get; } = "hugging-face";

    public string DisplayName { get; } = "Hugging Face";

    public async Task<CatalogSourceResult> LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync(
                "/api/models?pipeline_tag=text-generation&sort=downloads&direction=-1&limit=50",
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Unavailable();
            }

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return CatalogSourceResult.Available([]);
            }

            List<CatalogModelSummary> models = [];
            foreach (JsonElement modelElement in document.RootElement.EnumerateArray())
            {
                CatalogModelSummary? model = TryReadModel(modelElement);
                if (model is not null)
                {
                    models.Add(model);
                }
            }

            return CatalogSourceResult.Available(models);
        }
        catch (Exception exception) when (exception is HttpRequestException
                                        || exception is JsonException
                                        || exception is TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return Unavailable();
        }
    }

    private static CatalogSourceResult Unavailable() =>
        CatalogSourceResult.Unavailable(
            "Model catalog is unavailable.",
            "Go online and try syncing the catalog again.");

    private static CatalogModelSummary? TryReadModel(JsonElement modelElement)
    {
        if (!modelElement.TryGetProperty("id", out JsonElement idElement)
            || idElement.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        string id = idElement.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        IReadOnlyList<string> tags = ReadTags(modelElement);
        string pipelineTag = ReadStringProperty(modelElement, "pipeline_tag");

        return new CatalogModelSummary(
            id,
            ToDisplayName(id),
            tags,
            InferPurposes(id, pipelineTag, tags));
    }

    private static IReadOnlyList<string> ReadTags(JsonElement modelElement)
    {
        if (!modelElement.TryGetProperty("tags", out JsonElement tagsElement)
            || tagsElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        List<string> tags = [];
        foreach (JsonElement tagElement in tagsElement.EnumerateArray())
        {
            if (tagElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            string tag = tagElement.GetString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(tag))
            {
                tags.Add(tag);
            }
        }

        return tags;
    }

    private static string ReadStringProperty(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property)
            || property.ValueKind != JsonValueKind.String)
        {
            return string.Empty;
        }

        return property.GetString() ?? string.Empty;
    }

    private static string ToDisplayName(string id)
    {
        int slashIndex = id.LastIndexOf('/');

        return slashIndex >= 0 && slashIndex + 1 < id.Length
            ? id[(slashIndex + 1)..]
            : id;
    }

    private static IReadOnlyList<CatalogModelPurpose> InferPurposes(
        string id,
        string pipelineTag,
        IReadOnlyList<string> tags)
    {
        HashSet<CatalogModelPurpose> purposes = [];
        string searchableText = string.Join(
            ' ',
            [id, pipelineTag, .. tags]);

        if (ContainsAny(searchableText, "chat", "instruct", "conversational", "text-generation"))
        {
            purposes.Add(CatalogModelPurpose.GeneralChat);
        }

        if (ContainsAny(searchableText, "code", "coder", "coding"))
        {
            purposes.Add(CatalogModelPurpose.Coding);
        }

        if (ContainsAny(searchableText, "vision", "image-to-text", "visual-question-answering"))
        {
            purposes.Add(CatalogModelPurpose.Vision);
        }

        if (ContainsAny(searchableText, "embedding", "sentence-similarity", "feature-extraction"))
        {
            purposes.Add(CatalogModelPurpose.Embeddings);
        }

        if (purposes.Count == 0)
        {
            purposes.Add(CatalogModelPurpose.GeneralChat);
        }

        return purposes.ToArray();
    }

    private static bool ContainsAny(string text, params string[] values) =>
        values.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase));
}
