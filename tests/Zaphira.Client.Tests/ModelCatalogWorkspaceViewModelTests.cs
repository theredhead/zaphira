using Zaphira.Client.Chat;
using Zaphira.Client.ModelCatalog;
using Zaphira.Client.ViewModels;
using Zaphira.Contracts;

namespace Zaphira.Client.Tests;

public sealed class ModelCatalogWorkspaceViewModelTests
{
    [Fact]
    public async Task SearchAsyncLoadsCatalogModels()
    {
        FakeModelCatalogApiClient apiClient = new(CreateResponse("Catalog loaded."));
        ModelCatalogWorkspaceViewModel viewModel = new(apiClient)
        {
            SearchText = "coder",
            SelectedPurpose = "Coding"
        };

        await viewModel.SearchAsync(CancellationToken.None);

        Assert.Equal(1, apiClient.GetCatalogCallCount);
        Assert.Equal("coder", apiClient.LastQuery);
        Assert.Equal("Coding", apiClient.LastPurpose);
        CatalogModelItemViewModel model = Assert.Single(viewModel.Models);
        Assert.Equal("Qwen2.5-Coder-7B-Instruct", model.DisplayName);
        Assert.Equal("DirectlyUsable (Medium)", model.CompatibilityText);
        Assert.Equal("Catalog loaded.", viewModel.StatusText);
        Assert.False(viewModel.IsLoading);
    }

    [Fact]
    public async Task SyncNowAsyncRefreshesCatalog()
    {
        FakeModelCatalogApiClient apiClient = new(CreateResponse("Catalog loaded."));
        ModelCatalogWorkspaceViewModel viewModel = new(apiClient);

        await viewModel.SyncNowAsync(CancellationToken.None);

        Assert.Equal(1, apiClient.SyncCatalogCallCount);
        Assert.Single(viewModel.Models);
    }

    [Fact]
    public async Task SearchAsyncShowsCatalogUnavailableSuggestion()
    {
        FakeModelCatalogApiClient apiClient = new(
            failure: new ChatApiException(503, ErrorResponse.CatalogUnavailable()));
        ModelCatalogWorkspaceViewModel viewModel = new(apiClient);

        await viewModel.SearchAsync(CancellationToken.None);

        Assert.Equal(
            "The model catalog is unavailable. Go online and try syncing the catalog again.",
            viewModel.StatusText);
        Assert.False(viewModel.IsLoading);
    }

    private static ModelCatalogResponse CreateResponse(string message) =>
        new(
            isFromCache: false,
            message,
            "Search or filter the catalog.",
            [
                new CatalogModelResponse(
                    "Qwen/Qwen2.5-Coder-7B-Instruct",
                    "Qwen2.5-Coder-7B-Instruct",
                    ["code"],
                    ["Coding"],
                    "DirectlyUsable",
                    "Medium",
                    "Estimated model memory fits within available unified memory.",
                    "Matched name or id: coder.")
            ]);

    private sealed class FakeModelCatalogApiClient : IModelCatalogApiClient
    {
        private readonly ModelCatalogResponse response;
        private readonly ChatApiException failure;

        public FakeModelCatalogApiClient(ModelCatalogResponse? response = null, ChatApiException? failure = null)
        {
            this.response = response ?? CreateResponse("Catalog loaded.");
            this.failure = failure ?? ChatApiException.None;
            LastQuery = string.Empty;
            LastPurpose = string.Empty;
        }

        public int GetCatalogCallCount { get; private set; }

        public int SyncCatalogCallCount { get; private set; }

        public string LastQuery { get; private set; }

        public string LastPurpose { get; private set; }

        public Task<ModelCatalogResponse> GetCatalogAsync(
            string query,
            string purpose,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(purpose);
            cancellationToken.ThrowIfCancellationRequested();
            ThrowFailureIfNeeded();

            GetCatalogCallCount++;
            LastQuery = query;
            LastPurpose = purpose;

            return Task.FromResult(response);
        }

        public Task<ModelCatalogResponse> SyncCatalogAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowFailureIfNeeded();
            SyncCatalogCallCount++;

            return Task.FromResult(response);
        }

        private void ThrowFailureIfNeeded()
        {
            if (failure != ChatApiException.None)
            {
                throw failure;
            }
        }
    }
}
