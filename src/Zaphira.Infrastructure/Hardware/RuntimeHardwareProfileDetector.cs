using System.Runtime.InteropServices;
using Zaphira.Application.Hardware;

namespace Zaphira.Infrastructure.Hardware;

public sealed class RuntimeHardwareProfileDetector : IHardwareProfileDetector
{
    private readonly long memoryHeadroomBytes;

    public RuntimeHardwareProfileDetector(long memoryHeadroomBytes)
    {
        if (memoryHeadroomBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(memoryHeadroomBytes), "Memory headroom cannot be negative.");
        }

        this.memoryHeadroomBytes = memoryHeadroomBytes;
    }

    public async Task<HardwareProfile> DetectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Task.CompletedTask;

        return new HardwareProfile(
            RuntimeInformation.OSDescription,
            $"{RuntimeInformation.ProcessArchitecture}; {Environment.ProcessorCount} logical processors",
            GC.GetGCMemoryInfo().TotalAvailableMemoryBytes,
            "Unknown GPU",
            HasLikelyUnifiedMemory(),
            memoryHeadroomBytes);
    }

    private static bool HasLikelyUnifiedMemory() =>
        OperatingSystem.IsMacOS() && RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
}
