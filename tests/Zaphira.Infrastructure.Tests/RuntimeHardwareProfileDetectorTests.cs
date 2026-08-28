using Zaphira.Infrastructure.Hardware;

namespace Zaphira.Infrastructure.Tests;

public sealed class RuntimeHardwareProfileDetectorTests
{
    [Fact]
    public async Task DetectAsyncReturnsNonNullHardwareProfileValues()
    {
        RuntimeHardwareProfileDetector detector = new(memoryHeadroomBytes: 1024);

        Application.Hardware.HardwareProfile profile = await detector.DetectAsync(CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(profile.OperatingSystem));
        Assert.False(string.IsNullOrWhiteSpace(profile.Cpu));
        Assert.True(profile.PhysicalMemoryBytes >= 0);
        Assert.False(string.IsNullOrWhiteSpace(profile.Gpu));
        Assert.Equal(1024, profile.MemoryHeadroomBytes);
    }

    [Fact]
    public void ConstructorRejectsNegativeMemoryHeadroom()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RuntimeHardwareProfileDetector(-1));
    }
}
