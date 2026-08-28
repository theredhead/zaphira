namespace Zaphira.Client.Configuration;

public sealed record ZaphiraClientConfiguration
{
    public ZaphiraClientConfiguration(Uri backendAddress, bool startsInFirstRun)
    {
        ArgumentNullException.ThrowIfNull(backendAddress);

        if (!backendAddress.IsAbsoluteUri)
        {
            throw new ArgumentException("Backend address must be absolute.", nameof(backendAddress));
        }

        BackendAddress = backendAddress;
        StartsInFirstRun = startsInFirstRun;
    }

    public Uri BackendAddress { get; }

    public bool StartsInFirstRun { get; }

    public static ZaphiraClientConfiguration Default() =>
        new(new Uri("https://localhost:5051"), startsInFirstRun: false);
}
