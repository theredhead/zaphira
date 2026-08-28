using System.Net;
using System.Text;
using Zaphira.Application;
using Zaphira.Application.Providers;
using Zaphira.Domain;
using Zaphira.Infrastructure.Providers.Ollama;

namespace Zaphira.Infrastructure.Tests;

public sealed class OllamaChatModelProviderTests
{
    [Fact]
    public async Task CheckAvailabilityAsyncReturnsAvailableWhenOllamaResponds()
    {
        FakeHttpMessageHandler handler = new((request, _) => Task.FromResult(
            request.RequestUri!.AbsolutePath == "/api/version"
                ? JsonResponse(HttpStatusCode.OK, """{"version":"0.11.0"}""")
                : JsonResponse(HttpStatusCode.NotFound, "{}")));
        OllamaChatModelProvider provider = CreateProvider(handler);

        OllamaProviderAvailability availability = await provider.CheckAvailabilityAsync(CancellationToken.None);

        Assert.True(availability.IsAvailable);
        Assert.Equal("0.11.0", availability.Version);
    }

    [Fact]
    public async Task CheckAvailabilityAsyncReturnsUnavailableWhenOllamaDoesNotRespond()
    {
        FakeHttpMessageHandler handler = new((_, _) => Task.FromException<HttpResponseMessage>(new HttpRequestException("offline")));
        OllamaChatModelProvider provider = CreateProvider(handler);

        OllamaProviderAvailability availability = await provider.CheckAvailabilityAsync(CancellationToken.None);

        Assert.False(availability.IsAvailable);
        Assert.Equal("Ollama is unavailable.", availability.Message);
        Assert.Equal("Start Ollama or go online to install it, then try again.", availability.Suggestion);
    }

    [Fact]
    public async Task ListModelsAsyncReturnsInstalledModelsAndInspectsMetadata()
    {
        List<string> requestedPaths = [];
        FakeHttpMessageHandler handler = new((request, _) =>
        {
            requestedPaths.Add(request.RequestUri!.AbsolutePath);

            return Task.FromResult(request.RequestUri.AbsolutePath switch
            {
                "/api/tags" => JsonResponse(HttpStatusCode.OK, """{"models":[{"name":"llama3.2","size":123}]}"""),
                "/api/show" => JsonResponse(HttpStatusCode.OK, """{"capabilities":["completion","vision"]}"""),
                _ => JsonResponse(HttpStatusCode.NotFound, "{}")
            });
        });
        OllamaChatModelProvider provider = CreateProvider(handler);

        ProviderModelCatalog catalog = await provider.ListModelsAsync(CancellationToken.None);

        ProviderModelSummary model = Assert.Single(catalog.Models);
        Assert.Equal(new ProviderId("ollama"), catalog.ProviderId);
        Assert.Equal(new ModelId("llama3.2"), model.Id);
        Assert.True(model.Capabilities.Contains(ProviderCapability.TextGeneration));
        Assert.True(model.Capabilities.Contains(ProviderCapability.StreamingGeneration));
        Assert.True(model.Capabilities.Contains(ProviderCapability.ImageInput));
        Assert.Contains("/api/show", requestedPaths);
    }

    [Fact]
    public async Task GenerateAsyncStreamsTextAndCompletionEvents()
    {
        FakeHttpMessageHandler handler = new((request, _) => Task.FromResult(
            request.RequestUri!.AbsolutePath == "/api/chat"
                ? StreamResponse(
                    """
                    {"message":{"content":"Hel"},"done":false}
                    {"message":{"content":"lo"},"done":false}
                    {"done":true}
                    """)
                : JsonResponse(HttpStatusCode.NotFound, "{}")));
        OllamaChatModelProvider provider = CreateProvider(handler);

        List<ProviderGenerationEvent> events = [];
        await foreach (ProviderGenerationEvent generationEvent in provider.GenerateAsync(CreateRequest(), CancellationToken.None))
        {
            events.Add(generationEvent);
        }

        Assert.Equal("Hel", Assert.IsType<TextGenerationDeltaEvent>(events[0]).Text);
        Assert.Equal("lo", Assert.IsType<TextGenerationDeltaEvent>(events[1]).Text);
        Assert.IsType<GenerationCompletedEvent>(events[2]);
    }

    [Fact]
    public async Task GenerateAsyncReturnsFailureEventForProviderErrors()
    {
        FakeHttpMessageHandler handler = new((request, _) => Task.FromResult(
            request.RequestUri!.AbsolutePath == "/api/chat"
                ? JsonResponse(HttpStatusCode.ServiceUnavailable, """{"error":"model not found"}""")
                : JsonResponse(HttpStatusCode.NotFound, "{}")));
        OllamaChatModelProvider provider = CreateProvider(handler);

        List<ProviderGenerationEvent> events = [];
        await foreach (ProviderGenerationEvent generationEvent in provider.GenerateAsync(CreateRequest(), CancellationToken.None))
        {
            events.Add(generationEvent);
        }

        GenerationFailedEvent failed = Assert.IsType<GenerationFailedEvent>(Assert.Single(events));
        Assert.Equal("Ollama.GenerationFailed", failed.Error.Code);
        Assert.Equal("Ollama could not generate a response.", failed.Error.Message);
        Assert.Equal("Check that Ollama is running and the selected model is installed.", failed.Error.Suggestion);
    }

    [Fact]
    public async Task GenerateAsyncPropagatesCancellation()
    {
        FakeHttpMessageHandler handler = new(async (_, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            return StreamResponse("""{"done":true}""");
        });
        OllamaChatModelProvider provider = CreateProvider(handler);
        using CancellationTokenSource cancellationTokenSource = new();
        await cancellationTokenSource.CancelAsync();

        IAsyncEnumerable<ProviderGenerationEvent> events = provider.GenerateAsync(CreateRequest(), cancellationTokenSource.Token);

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (ProviderGenerationEvent _ in events)
            {
            }
        });
    }

    [Fact]
    public async Task InstallModelAsyncStreamsProgressAndCompletionEvents()
    {
        FakeHttpMessageHandler handler = new((request, _) => Task.FromResult(
            request.RequestUri!.AbsolutePath == "/api/pull"
                ? StreamResponse(
                    """
                    {"status":"pulling manifest"}
                    {"status":"downloading digest","completed":42,"total":100}
                    {"status":"success"}
                    """)
                : JsonResponse(HttpStatusCode.NotFound, "{}")));
        OllamaChatModelProvider provider = CreateProvider(handler);

        List<ProviderModelInstallationEvent> events = [];
        await foreach (ProviderModelInstallationEvent installationEvent in provider.InstallModelAsync(
            new ModelId("llama3.2"),
            CancellationToken.None))
        {
            events.Add(installationEvent);
        }

        ProviderModelInstallationProgressEvent first = Assert.IsType<ProviderModelInstallationProgressEvent>(events[0]);
        ProviderModelInstallationProgressEvent second = Assert.IsType<ProviderModelInstallationProgressEvent>(events[1]);
        Assert.Equal("pulling manifest", first.Status);
        Assert.False(first.HasKnownTotalBytes);
        Assert.Equal(42, second.CompletedBytes);
        Assert.Equal(100, second.TotalBytes);
        Assert.True(second.HasKnownTotalBytes);
        Assert.IsType<ProviderModelInstallationCompletedEvent>(events[2]);
    }

    [Fact]
    public async Task InstallModelAsyncReturnsFailureEventForProviderError()
    {
        FakeHttpMessageHandler handler = new((request, _) => Task.FromResult(
            request.RequestUri!.AbsolutePath == "/api/pull"
                ? StreamResponse("""{"error":"not enough disk space"}""")
                : JsonResponse(HttpStatusCode.NotFound, "{}")));
        OllamaChatModelProvider provider = CreateProvider(handler);

        List<ProviderModelInstallationEvent> events = [];
        await foreach (ProviderModelInstallationEvent installationEvent in provider.InstallModelAsync(
            new ModelId("llama3.2"),
            CancellationToken.None))
        {
            events.Add(installationEvent);
        }

        ProviderModelInstallationFailedEvent failed = Assert.IsType<ProviderModelInstallationFailedEvent>(Assert.Single(events));
        Assert.Equal("Ollama.InstallationFailed", failed.Error.Code);
        Assert.Equal("not enough disk space", failed.Error.Suggestion);
    }

    [Fact]
    public async Task RemoveModelAsyncSendsDeleteRequest()
    {
        HttpMethod requestedMethod = HttpMethod.Get;
        string requestedPath = string.Empty;
        string requestBody = string.Empty;
        FakeHttpMessageHandler handler = new(async (request, cancellationToken) =>
        {
            requestedMethod = request.Method;
            requestedPath = request.RequestUri!.AbsolutePath;
            requestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return JsonResponse(HttpStatusCode.OK, "{}");
        });
        OllamaChatModelProvider provider = CreateProvider(handler);

        OperationResult result = await provider.RemoveModelAsync(new ModelId("llama3.2"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Delete, requestedMethod);
        Assert.Equal("/api/delete", requestedPath);
        Assert.Contains("llama3.2", requestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RemoveModelAsyncReturnsFailureForProviderError()
    {
        FakeHttpMessageHandler handler = new((_, _) => Task.FromResult(JsonResponse(HttpStatusCode.InternalServerError, "{}")));
        OllamaChatModelProvider provider = CreateProvider(handler);

        OperationResult result = await provider.RemoveModelAsync(new ModelId("llama3.2"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Ollama.RemoveModelFailed", result.Error.Code);
    }

    private static OllamaChatModelProvider CreateProvider(HttpMessageHandler handler) =>
        new(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434")
        });

    private static ProviderGenerationRequest CreateRequest() =>
        new(
            new ModelId("llama3.2"),
            [
                new ChatMessage(
                    MessageId.New(),
                    ConversationId.New(),
                    MessageRole.User,
                    [new TextMessagePart("Hello")],
                    MessageStatus.Completed,
                    DateTimeOffset.UtcNow)
            ]);

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage StreamResponse(string content) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/x-ndjson")
        };

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handleAsync;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handleAsync)
        {
            this.handleAsync = handleAsync;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return handleAsync(request, cancellationToken);
        }
    }
}
