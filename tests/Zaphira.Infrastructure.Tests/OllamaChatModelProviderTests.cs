using System.Net;
using System.Text;
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
