namespace Zaphira.Contracts;

public sealed record HealthResponse
{
    public HealthResponse(string serviceName, string status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        ServiceName = serviceName;
        Status = status;
    }

    public string ServiceName { get; }

    public string Status { get; }
}
