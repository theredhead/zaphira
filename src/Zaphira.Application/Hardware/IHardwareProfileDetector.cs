namespace Zaphira.Application.Hardware;

public interface IHardwareProfileDetector
{
    Task<HardwareProfile> DetectAsync(CancellationToken cancellationToken);
}
