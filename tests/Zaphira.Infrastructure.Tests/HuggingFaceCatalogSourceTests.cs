using System.Net;
using System.Text;
using Zaphira.Application.ModelCatalog;
using Zaphira.Infrastructure.ModelCatalog;

namespace Zaphira.Infrastructure.Tests;

public sealed class HuggingFaceCatalogSourceTests
{
    [Fact]
    public async Task LoadAsyncReturnsModelsFromHuggingFaceApi()
    {
        FakeHttpMessageHandler handler = new((request, _) =>
        {
            Assert.Equal("/api/models", request.RequestUri!.AbsolutePath);

            return Task.FromResult(JsonResponse(
                HttpStatusCode.OK,
                """
                [
                  {
                    "id": "microsoft/phi-4",
                    "pipeline_tag": "text-generation",
                    "tags": ["text-generation", "conversational"]
                  },
                  {
                    "id": "Qwen/Qwen2.5-Coder-7B-Instruct",
                    "pipeline_tag": "text-generation",
                    "tags": ["text-generation", "code"]
                  }
                ]
                """));
        });
        HuggingFaceCatalogSource source = CreateSource(handler);

        CatalogSourceResult result = await source.LoadAsync(CancellationToken.None);

        Assert.True(result.IsAvailable);
        Assert.Equal(2, result.Models.Count);
        Assert.Equal("microsoft/phi-4", result.Models[0].Id);
        Assert.Equal("phi-4", result.Models[0].DisplayName);
        Assert.Contains(CatalogModelPurpose.GeneralChat, result.Models[0].Purposes);
        Assert.Contains(CatalogModelPurpose.Coding, result.Models[1].Purposes);
    }

    [Fact]
    public async Task LoadAsyncSkipsModelsWithoutUsableIdentifiers()
    {
        FakeHttpMessageHandler handler = new((_, _) => Task.FromResult(JsonResponse(
            HttpStatusCode.OK,
            """
            [
              { "id": "   ", "pipeline_tag": "text-generation", "tags": ["text-generation"] },
              { "pipeline_tag": "text-generation", "tags": ["text-generation"] },
              { "id": "valid/model", "pipeline_tag": "text-generation", "tags": ["text-generation"] }
            ]
            """)));
        HuggingFaceCatalogSource source = CreateSource(handler);

        CatalogSourceResult result = await source.LoadAsync(CancellationToken.None);

        CatalogModelSummary model = Assert.Single(result.Models);
        Assert.Equal("valid/model", model.Id);
    }

    [Fact]
    public async Task LoadAsyncReturnsUnavailableWhenHuggingFaceCannotBeReached()
    {
        FakeHttpMessageHandler handler = new((_, _) =>
            Task.FromException<HttpResponseMessage>(new HttpRequestException("offline")));
        HuggingFaceCatalogSource source = CreateSource(handler);

        CatalogSourceResult result = await source.LoadAsync(CancellationToken.None);

        Assert.False(result.IsAvailable);
        Assert.Empty(result.Models);
        Assert.Equal("Model catalog is unavailable.", result.Message);
        Assert.Equal("Go online and try syncing the catalog again.", result.Suggestion);
    }

    private static HuggingFaceCatalogSource CreateSource(HttpMessageHandler handler) =>
        new(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://huggingface.co")
        });

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handleAsync;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handleAsync)
        {
            ArgumentNullException.ThrowIfNull(handleAsync);

            this.handleAsync = handleAsync;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return handleAsync(request, cancellationToken);
        }
    }
}
