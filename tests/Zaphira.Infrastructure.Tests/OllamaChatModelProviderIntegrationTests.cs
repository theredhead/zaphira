using Zaphira.Infrastructure.Providers.Ollama;

namespace Zaphira.Infrastructure.Tests;

public sealed class OllamaChatModelProviderIntegrationTests
{
    [Fact]
    public async Task ListModelsAsyncCanRunAgainstLocalOllamaWhenAvailable()
    {
        using HttpClient httpClient = new()
        {
            BaseAddress = new Uri("http://localhost:11434"),
            Timeout = TimeSpan.FromSeconds(1)
        };
        OllamaChatModelProvider provider = new(httpClient);

        OllamaProviderAvailability availability = await provider.CheckAvailabilityAsync(CancellationToken.None);
        if (!availability.IsAvailable)
        {
            return;
        }

        await provider.ListModelsAsync(CancellationToken.None);
    }
}
