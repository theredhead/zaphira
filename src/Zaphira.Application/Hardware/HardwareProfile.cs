namespace Zaphira.Application.Hardware;

public sealed record HardwareProfile
{
    public HardwareProfile(
        string operatingSystem,
        string cpu,
        long physicalMemoryBytes,
        string gpu,
        bool hasUnifiedMemory,
        long memoryHeadroomBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operatingSystem);
        ArgumentException.ThrowIfNullOrWhiteSpace(cpu);
        ArgumentException.ThrowIfNullOrWhiteSpace(gpu);

        if (physicalMemoryBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(physicalMemoryBytes), "Physical memory cannot be negative.");
        }

        if (memoryHeadroomBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(memoryHeadroomBytes), "Memory headroom cannot be negative.");
        }

        OperatingSystem = operatingSystem;
        Cpu = cpu;
        PhysicalMemoryBytes = physicalMemoryBytes;
        Gpu = gpu;
        HasUnifiedMemory = hasUnifiedMemory;
        MemoryHeadroomBytes = memoryHeadroomBytes;
    }

    public string OperatingSystem { get; }

    public string Cpu { get; }

    public long PhysicalMemoryBytes { get; }

    public string Gpu { get; }

    public bool HasUnifiedMemory { get; }

    public long MemoryHeadroomBytes { get; }

    public long AvailableMemoryBytes => Math.Max(0, PhysicalMemoryBytes - MemoryHeadroomBytes);
}
