namespace Zaphira.Server.Pairing;

internal sealed record ServerPairingCreationResult
{
    private ServerPairingCreationResult(bool isCreated, ServerPairing pairing)
    {
        ArgumentNullException.ThrowIfNull(pairing);

        IsCreated = isCreated;
        Pairing = pairing;
    }

    public bool IsCreated { get; }

    public ServerPairing Pairing { get; }

    public static ServerPairingCreationResult Created(ServerPairing pairing) => new(true, pairing);

    public static ServerPairingCreationResult InvalidCode() => new(false, EmptyPairing);

    private static ServerPairing EmptyPairing { get; } = new(
        Guid.Parse("00000000-0000-0000-0000-000000000001"),
        "__zaphira_no_pairing_client__",
        "__zaphira_no_pairing_token__",
        DateTimeOffset.UnixEpoch,
        isRevoked: true,
        DateTimeOffset.UnixEpoch);
}
