using Zaphira.Application.ModelCatalog;

namespace Zaphira.Application.Tests;

public sealed class CatalogSearchServiceTests
{
    [Fact]
    public void SearchMatchesModelNameAndExplainsMatch()
    {
        CatalogSearchService service = new();

        IReadOnlyList<CatalogModelSearchResult> results = service.Search(
            CreateModels(),
            new CatalogSearchRequest("coder", []));

        CatalogModelSearchResult result = Assert.Single(results);
        Assert.Equal("Qwen/Qwen2.5-Coder-7B-Instruct", result.Model.Id);
        Assert.Equal("Matched name or id: coder.", result.MatchExplanation);
    }

    [Fact]
    public void SearchFiltersByPurpose()
    {
        CatalogSearchService service = new();

        IReadOnlyList<CatalogModelSearchResult> results = service.Search(
            CreateModels(),
            new CatalogSearchRequest(string.Empty, [CatalogModelPurpose.Coding]));

        CatalogModelSearchResult result = Assert.Single(results);
        Assert.Contains(CatalogModelPurpose.Coding, result.Model.Purposes);
        Assert.Equal("Matched purpose: Coding.", result.MatchExplanation);
    }

    [Fact]
    public void SearchReturnsCompatibilityStatusAndConfidence()
    {
        CatalogSearchService service = new();

        IReadOnlyList<CatalogModelSearchResult> results = service.Search(
            CreateModels(),
            CatalogSearchRequest.All());

        CatalogModelSearchResult chat = results.Single(result => result.Model.Id == "microsoft/phi-4");
        CatalogModelSearchResult embeddings = results.Single(result => result.Model.Id == "sentence-transformers/all-MiniLM-L6-v2");

        Assert.Equal(CatalogCompatibilityStatus.DirectlyUsable, chat.CompatibilityStatus);
        Assert.Equal(CatalogCompatibilityConfidence.Medium, chat.CompatibilityConfidence);
        Assert.Equal(CatalogCompatibilityStatus.Unsupported, embeddings.CompatibilityStatus);
        Assert.Equal(CatalogCompatibilityConfidence.High, embeddings.CompatibilityConfidence);
    }

    private static IReadOnlyList<CatalogModelSummary> CreateModels() =>
    [
        new CatalogModelSummary(
            "microsoft/phi-4",
            "phi-4",
            ["text-generation"],
            [CatalogModelPurpose.GeneralChat]),
        new CatalogModelSummary(
            "Qwen/Qwen2.5-Coder-7B-Instruct",
            "Qwen2.5-Coder-7B-Instruct",
            ["code"],
            [CatalogModelPurpose.Coding]),
        new CatalogModelSummary(
            "sentence-transformers/all-MiniLM-L6-v2",
            "all-MiniLM-L6-v2",
            ["sentence-similarity"],
            [CatalogModelPurpose.Embeddings])
    ];
}
