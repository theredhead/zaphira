using Zaphira.Application.Hardware;
using Zaphira.Application.ModelCatalog;

namespace Zaphira.Application.Tests;

public sealed class CatalogCompatibilityEstimatorTests
{
    [Fact]
    public void EstimateReturnsDirectlyUsableWhenModelFitsAvailableMemory()
    {
        CatalogCompatibilityEstimator estimator = new();

        CatalogModelSearchResult result = estimator.Estimate(
            Model("microsoft/phi-4-7b"),
            Hardware(memoryGibibytes: 16, headroomGibibytes: 2));

        Assert.Equal(CatalogCompatibilityStatus.DirectlyUsable, result.CompatibilityStatus);
        Assert.Equal(CatalogCompatibilityConfidence.Medium, result.CompatibilityConfidence);
        Assert.Contains("fits", result.MatchExplanation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EstimateReturnsPossiblyUsableWhenModelBarelyFits()
    {
        CatalogCompatibilityEstimator estimator = new();

        CatalogModelSearchResult result = estimator.Estimate(
            Model("microsoft/phi-4-7b"),
            Hardware(memoryGibibytes: 11, headroomGibibytes: 1));

        Assert.Equal(CatalogCompatibilityStatus.PossiblyUsable, result.CompatibilityStatus);
        Assert.Equal(CatalogCompatibilityConfidence.Medium, result.CompatibilityConfidence);
        Assert.Contains("close", result.MatchExplanation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EstimateReturnsUnsupportedWhenModelExceedsAvailableMemory()
    {
        CatalogCompatibilityEstimator estimator = new();

        CatalogModelSearchResult result = estimator.Estimate(
            Model("meta-llama/Llama-3-70B"),
            Hardware(memoryGibibytes: 16, headroomGibibytes: 2));

        Assert.Equal(CatalogCompatibilityStatus.Unsupported, result.CompatibilityStatus);
        Assert.Equal(CatalogCompatibilityConfidence.High, result.CompatibilityConfidence);
        Assert.Contains("exceeds", result.MatchExplanation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EstimateReturnsUnknownWhenModelSizeOrMemoryIsUnknown()
    {
        CatalogCompatibilityEstimator estimator = new();

        CatalogModelSearchResult result = estimator.Estimate(
            Model("unknown/model"),
            Hardware(memoryGibibytes: 0, headroomGibibytes: 0));

        Assert.Equal(CatalogCompatibilityStatus.Unknown, result.CompatibilityStatus);
        Assert.Equal(CatalogCompatibilityConfidence.Low, result.CompatibilityConfidence);
        Assert.Contains("uncertain", result.MatchExplanation, StringComparison.OrdinalIgnoreCase);
    }

    private static CatalogModelSummary Model(string id) =>
        new(id, id, ["text-generation"], [CatalogModelPurpose.GeneralChat]);

    private static HardwareProfile Hardware(int memoryGibibytes, int headroomGibibytes) =>
        new(
            "Test OS",
            "Test CPU",
            Gibibytes(memoryGibibytes),
            "Test GPU",
            hasUnifiedMemory: true,
            Gibibytes(headroomGibibytes));

    private static long Gibibytes(int value) => value * 1024L * 1024L * 1024L;
}
