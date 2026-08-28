using System.Security.Cryptography;
using System.Text;
using Zaphira.Contracts;

namespace Zaphira.Server.Pairing;

internal sealed class ServerPairingRegistry
{
    private static readonly TimeSpan PairingCodeLifetime = TimeSpan.FromMinutes(10);

    private readonly ServerPairingStore store;
    private readonly TimeProvider timeProvider;
    private readonly object syncRoot = new();
    private PairingCode activePairingCode = PairingCode.None;

    public ServerPairingRegistry(ServerPairingStore store, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(timeProvider);

        this.store = store;
        this.timeProvider = timeProvider;
    }

    public PairingCode CreatePairingCode()
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        PairingCode code = new(CreateFourDigitCode(), now.Add(PairingCodeLifetime));

        lock (syncRoot)
        {
            activePairingCode = code;
        }

        return code;
    }

    public async Task<ServerPairingCreationResult> CreatePairingAsync(
        string code,
        string clientName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);

        PairingCode pairingCode;
        lock (syncRoot)
        {
            pairingCode = activePairingCode;
        }

        if (pairingCode == PairingCode.None
            || pairingCode.IsExpired(timeProvider.GetUtcNow())
            || pairingCode.Value != code)
        {
            return ServerPairingCreationResult.InvalidCode();
        }

        ServerPairing pairing = new(
            Guid.NewGuid(),
            clientName,
            CreateAccessToken(),
            timeProvider.GetUtcNow(),
            isRevoked: false,
            PairingResponse.NotRevokedAt);

        List<ServerPairing> pairings = [.. await store.LoadAsync(cancellationToken), pairing];
        await store.SaveAsync(pairings, cancellationToken);

        lock (syncRoot)
        {
            activePairingCode = PairingCode.None;
        }

        return ServerPairingCreationResult.Created(pairing);
    }

    public Task<IReadOnlyList<ServerPairing>> ListPairingsAsync(CancellationToken cancellationToken) =>
        store.LoadAsync(cancellationToken);

    public async Task<bool> RevokePairingAsync(Guid pairingId, CancellationToken cancellationToken)
    {
        if (pairingId == Guid.Empty)
        {
            throw new ArgumentException("Pairing identifier cannot be empty.", nameof(pairingId));
        }

        List<ServerPairing> pairings = [.. await store.LoadAsync(cancellationToken)];
        int pairingIndex = pairings.FindIndex(pairing => pairing.Id == pairingId);
        if (pairingIndex < 0)
        {
            return false;
        }

        ServerPairing existing = pairings[pairingIndex];
        pairings[pairingIndex] = new ServerPairing(
            existing.Id,
            existing.ClientName,
            existing.AccessToken,
            existing.CreatedAt,
            isRevoked: true,
            timeProvider.GetUtcNow());

        await store.SaveAsync(pairings, cancellationToken);

        return true;
    }

    public async Task<bool> HasPairingsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ServerPairing> pairings = await store.LoadAsync(cancellationToken);

        return pairings.Count > 0;
    }

    public async Task<bool> IsAccessTokenAuthorizedAsync(string accessToken, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        IReadOnlyList<ServerPairing> pairings = await store.LoadAsync(cancellationToken);

        return pairings.Any(pairing =>
            !pairing.IsRevoked && AreEqual(pairing.AccessToken, accessToken));
    }

    private static string CreateFourDigitCode() =>
        RandomNumberGenerator.GetInt32(0, 10_000).ToString("D4");

    private static string CreateAccessToken()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(bytes);
    }

    private static bool AreEqual(string left, string right)
    {
        byte[] leftBytes = Encoding.UTF8.GetBytes(left);
        byte[] rightBytes = Encoding.UTF8.GetBytes(right);

        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
