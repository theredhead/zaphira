using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Zaphira.Application.Providers;
using Zaphira.Domain;

namespace Zaphira.Infrastructure.Providers.Ollama;

public sealed class OllamaChatModelProvider : IChatModelProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly ProviderCapabilities TextModelCapabilities =
        new([ProviderCapability.TextGeneration, ProviderCapability.StreamingGeneration]);

    private readonly HttpClient httpClient;

    public OllamaChatModelProvider(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        this.httpClient = httpClient;
    }

    public ProviderId Id { get; } = new("ollama");

    public string DisplayName { get; } = "Ollama";

    public ProviderCapabilities Capabilities { get; } =
        new([ProviderCapability.TextGeneration, ProviderCapability.StreamingGeneration, ProviderCapability.ImageInput]);

    public async Task<OllamaProviderAvailability> CheckAvailabilityAsync(CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync("/api/version", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return OllamaProviderAvailability.Unavailable();
            }

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            string version = document.RootElement.TryGetProperty("version", out JsonElement versionElement)
                ? versionElement.GetString() ?? "Unknown"
                : "Unknown";

            return OllamaProviderAvailability.Available(version);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return OllamaProviderAvailability.Unavailable();
        }
    }

    public async Task<ProviderModelCatalog> ListModelsAsync(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.GetAsync("/api/tags", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new ProviderModelCatalog(Id, []);
        }

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        List<ProviderModelSummary> models = [];
        if (!document.RootElement.TryGetProperty("models", out JsonElement modelsElement)
            || modelsElement.ValueKind != JsonValueKind.Array)
        {
            return new ProviderModelCatalog(Id, models);
        }

        foreach (JsonElement modelElement in modelsElement.EnumerateArray())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!modelElement.TryGetProperty("name", out JsonElement nameElement))
            {
                continue;
            }

            string modelName = nameElement.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(modelName))
            {
                continue;
            }

            ProviderCapabilities capabilities = await InspectModelCapabilitiesAsync(modelName, cancellationToken);
            models.Add(new ProviderModelSummary(new ModelId(modelName), modelName, capabilities));
        }

        return new ProviderModelCatalog(Id, models);
    }

    public async IAsyncEnumerable<ProviderGenerationEvent> GenerateAsync(
        ProviderGenerationRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        using HttpResponseMessage response = await httpClient.SendAsync(
            CreateChatRequest(request),
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            yield return new GenerationFailedEvent(new ProviderError(
                "Ollama.GenerationFailed",
                "Ollama could not generate a response.",
                "Check that Ollama is running and the selected model is installed."));
            yield break;
        }

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using StreamReader reader = new(stream, Encoding.UTF8);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            ProviderGenerationEvent generationEvent = ParseGenerationEvent(line);
            yield return generationEvent;

            if (generationEvent is GenerationCompletedEvent)
            {
                yield break;
            }
        }
    }

    private async Task<ProviderCapabilities> InspectModelCapabilitiesAsync(
        string modelName,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "/api/show",
            new { model = modelName },
            SerializerOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return TextModelCapabilities;
        }

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        List<ProviderCapability> capabilities =
        [
            ProviderCapability.TextGeneration,
            ProviderCapability.StreamingGeneration
        ];

        if (document.RootElement.TryGetProperty("capabilities", out JsonElement capabilitiesElement)
            && capabilitiesElement.ValueKind == JsonValueKind.Array
            && capabilitiesElement.EnumerateArray().Any(IsVisionCapability))
        {
            capabilities.Add(ProviderCapability.ImageInput);
        }

        return new ProviderCapabilities(capabilities);
    }

    private static bool IsVisionCapability(JsonElement capabilityElement)
    {
        string capability = capabilityElement.GetString() ?? string.Empty;

        return capability.Contains("vision", StringComparison.OrdinalIgnoreCase)
            || capability.Contains("image", StringComparison.OrdinalIgnoreCase);
    }

    private static HttpRequestMessage CreateChatRequest(ProviderGenerationRequest request)
    {
        object body = new
        {
            model = request.ModelId.Value,
            stream = true,
            messages = request.Messages.Select(message => new
            {
                role = message.Role.ToString().ToLowerInvariant(),
                content = GetMessageTextContent(message)
            })
        };

        return new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = JsonContent.Create(body, options: SerializerOptions)
        };
    }

    private static string GetMessageTextContent(ChatMessage message)
    {
        string content = string.Join(
            Environment.NewLine,
            message.Parts.OfType<TextMessagePart>().Select(part => part.Text));

        return string.IsNullOrWhiteSpace(content)
            ? "[non-text message parts omitted]"
            : content;
    }

    private static ProviderGenerationEvent ParseGenerationEvent(string line)
    {
        using JsonDocument document = JsonDocument.Parse(line);
        JsonElement root = document.RootElement;

        if (root.TryGetProperty("error", out JsonElement errorElement))
        {
            string error = errorElement.GetString() ?? "Unknown Ollama error.";

            return new GenerationFailedEvent(new ProviderError(
                "Ollama.GenerationFailed",
                "Ollama could not generate a response.",
                error));
        }

        if (root.TryGetProperty("message", out JsonElement messageElement)
            && messageElement.TryGetProperty("content", out JsonElement contentElement))
        {
            string content = contentElement.GetString() ?? string.Empty;
            if (!string.IsNullOrEmpty(content))
            {
                return new TextGenerationDeltaEvent(content);
            }
        }

        bool done = root.TryGetProperty("done", out JsonElement doneElement) && doneElement.GetBoolean();

        return done
            ? GenerationCompletedEvent.Instance
            : new TextGenerationDeltaEvent(" ");
    }
}
