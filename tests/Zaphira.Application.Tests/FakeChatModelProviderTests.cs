using System.Runtime.CompilerServices;
using Zaphira.Application;
using Zaphira.Application.Providers;
using Zaphira.Domain;

namespace Zaphira.Application.Tests;

public sealed class FakeChatModelProviderTests
{
    [Fact]
    public async Task ListModelsAsyncReturnsProviderModelCatalog()
    {
        FakeChatModelProvider provider = new();

        ProviderModelCatalog catalog = await provider.ListModelsAsync(CancellationToken.None);

        Assert.Equal(new ProviderId("fake"), catalog.ProviderId);
        ProviderModelSummary model = Assert.Single(catalog.Models);
        Assert.Equal(new ModelId("fake-chat"), model.Id);
        Assert.True(model.Capabilities.Contains(ProviderCapability.TextGeneration));
    }

    [Fact]
    public async Task GenerateAsyncReturnsGenerationEvents()
    {
        FakeChatModelProvider provider = new();
        ProviderGenerationRequest request = CreateRequest("Hello");

        List<ProviderGenerationEvent> events = [];
        await foreach (ProviderGenerationEvent generationEvent in provider.GenerateAsync(request, CancellationToken.None))
        {
            events.Add(generationEvent);
        }

        Assert.Equal(2, events.Count);
        TextGenerationDeltaEvent delta = Assert.IsType<TextGenerationDeltaEvent>(events[0]);
        Assert.Equal("Fake response to: Hello", delta.Text);
        Assert.IsType<GenerationCompletedEvent>(events[1]);
    }

    [Fact]
    public async Task GenerateAsyncHonorsCancellation()
    {
        FakeChatModelProvider provider = new();
        using CancellationTokenSource cancellationTokenSource = new();
        await cancellationTokenSource.CancelAsync();

        IAsyncEnumerable<ProviderGenerationEvent> events = provider.GenerateAsync(
            CreateRequest("Hello"),
            cancellationTokenSource.Token);

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (ProviderGenerationEvent _ in events)
            {
            }
        });
    }

    [Fact]
    public async Task InstallModelAsyncReturnsProgressAndCompletion()
    {
        FakeChatModelProvider provider = new();

        List<ProviderModelInstallationEvent> events = [];
        await foreach (ProviderModelInstallationEvent installationEvent in provider.InstallModelAsync(
            new ModelId("fake-chat"),
            CancellationToken.None))
        {
            events.Add(installationEvent);
        }

        ProviderModelInstallationProgressEvent progress = Assert.IsType<ProviderModelInstallationProgressEvent>(events[0]);
        Assert.Equal(new ModelId("fake-chat"), progress.ModelId);
        Assert.Equal("Installing", progress.Status);
        Assert.IsType<ProviderModelInstallationCompletedEvent>(events[1]);
    }

    [Fact]
    public async Task RemoveModelAsyncReturnsSuccess()
    {
        FakeChatModelProvider provider = new();

        OperationResult result = await provider.RemoveModelAsync(new ModelId("fake-chat"), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void GenerationRequestRejectsEmptyMessages()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new ProviderGenerationRequest(new ModelId("fake-chat"), []));

        Assert.Equal("messages", exception.ParamName);
    }

    [Fact]
    public void ProviderErrorRejectsNoneCode()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new ProviderError(" ", "Provider failed.", "Try again."));

        Assert.Equal("code", exception.ParamName);
    }

    private static ProviderGenerationRequest CreateRequest(string text) =>
        new(
            new ModelId("fake-chat"),
            [
                new ChatMessage(
                    MessageId.New(),
                    ConversationId.New(),
                    MessageRole.User,
                    [new TextMessagePart(text)],
                    MessageStatus.Completed,
                    DateTimeOffset.UtcNow)
            ]);

    private sealed class FakeChatModelProvider : IChatModelProvider
    {
        public ProviderId Id { get; } = new("fake");

        public string DisplayName { get; } = "Fake Provider";

        public ProviderCapabilities Capabilities { get; } =
            new([ProviderCapability.TextGeneration, ProviderCapability.StreamingGeneration]);

        public Task<ProviderModelCatalog> ListModelsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ProviderModelSummary model = new(
                new ModelId("fake-chat"),
                "Fake Chat",
                new ProviderCapabilities([ProviderCapability.TextGeneration, ProviderCapability.StreamingGeneration]));

            return Task.FromResult(new ProviderModelCatalog(Id, [model]));
        }

        public async IAsyncEnumerable<ProviderModelInstallationEvent> InstallModelAsync(
            ModelId modelId,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(modelId);
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Yield();
            yield return new ProviderModelInstallationProgressEvent(modelId, "Installing", 1, 2, true);
            yield return ProviderModelInstallationCompletedEvent.Instance;
        }

        public Task<OperationResult> RemoveModelAsync(ModelId modelId, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(modelId);
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(OperationResult.Success());
        }

        public async IAsyncEnumerable<ProviderGenerationEvent> GenerateAsync(
            ProviderGenerationRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            TextMessagePart prompt = request.Messages
                .SelectMany(message => message.Parts)
                .OfType<TextMessagePart>()
                .Last();

            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return new TextGenerationDeltaEvent($"Fake response to: {prompt.Text}");

            cancellationToken.ThrowIfCancellationRequested();
            yield return GenerationCompletedEvent.Instance;
        }
    }
}
