using System.Globalization;
using System.Text.RegularExpressions;
using Zaphira.Application.Hardware;

namespace Zaphira.Application.ModelCatalog;

public sealed partial class CatalogCompatibilityEstimator
{
    public CatalogModelSearchResult Estimate(CatalogModelSummary model, HardwareProfile hardwareProfile)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(hardwareProfile);

        long estimatedMemoryBytes = EstimateRequiredMemoryBytes(model);
        if (estimatedMemoryBytes == 0 || hardwareProfile.PhysicalMemoryBytes == 0)
        {
            return new CatalogModelSearchResult(
                model,
                CatalogCompatibilityStatus.Unknown,
                CatalogCompatibilityConfidence.Low,
                "Compatibility is uncertain because model size or system memory is unknown.");
        }

        if (hardwareProfile.AvailableMemoryBytes < estimatedMemoryBytes)
        {
            return new CatalogModelSearchResult(
                model,
                CatalogCompatibilityStatus.Unsupported,
                CatalogCompatibilityConfidence.High,
                "Estimated model memory exceeds available system memory after headroom.");
        }

        if (hardwareProfile.AvailableMemoryBytes < estimatedMemoryBytes + Gibibytes(2))
        {
            return new CatalogModelSearchResult(
                model,
                CatalogCompatibilityStatus.PossiblyUsable,
                CatalogCompatibilityConfidence.Medium,
                "Estimated model memory is close to available memory; performance may be limited.");
        }

        return new CatalogModelSearchResult(
            model,
            CatalogCompatibilityStatus.DirectlyUsable,
            CatalogCompatibilityConfidence.Medium,
            hardwareProfile.HasUnifiedMemory
                ? "Estimated model memory fits within available unified memory."
                : "Estimated model memory fits within available system memory.");
    }

    private static long EstimateRequiredMemoryBytes(CatalogModelSummary model)
    {
        Match match = ParameterCountRegex().Match(model.Id);
        if (!match.Success)
        {
            match = ParameterCountRegex().Match(model.DisplayName);
        }

        if (!match.Success)
        {
            return 0;
        }

        decimal billionsOfParameters = decimal.Parse(match.Groups["parameters"].Value, CultureInfo.InvariantCulture);
        decimal conservativeBytes = billionsOfParameters * Gibibytes(1) + Gibibytes(2);

        return (long)conservativeBytes;
    }

    private static long Gibibytes(int value) => value * 1024L * 1024L * 1024L;

    [GeneratedRegex(@"(?<parameters>\d+(?:\.\d+)?)\s*b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ParameterCountRegex();
}
