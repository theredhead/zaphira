using System.Text.Json;
using Zaphira.Contracts;
using Zaphira.Infrastructure.Storage;

namespace Zaphira.Server.Pairing;

internal sealed class ServerPairingStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly ZaphiraDataDirectories dataDirectories;

    public ServerPairingStore(ZaphiraDataDirectories dataDirectories)
    {
        ArgumentNullException.ThrowIfNull(dataDirectories);

        this.dataDirectories = dataDirectories;
    }

    public async Task<IReadOnlyList<ServerPairing>> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(dataDirectories.ServerPairingsRoot);

        string file = PairingsFile;
        if (!File.Exists(file))
        {
            return [];
        }

        await using FileStream stream = File.OpenRead(file);
        List<ServerPairingFileItem>? items =
            await JsonSerializer.DeserializeAsync<List<ServerPairingFileItem>>(stream, SerializerOptions, cancellationToken);

        if (items is null)
        {
            throw new InvalidOperationException("Server pairings file did not contain pairing records.");
        }

        return items.Select(item => item.ToPairing()).ToArray();
    }

    public async Task SaveAsync(IReadOnlyList<ServerPairing> pairings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pairings);
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(dataDirectories.ServerPairingsRoot);

        ServerPairingFileItem[] items = pairings
            .Select(ServerPairingFileItem.FromPairing)
            .ToArray();

        await using FileStream stream = File.Create(PairingsFile);
        await JsonSerializer.SerializeAsync(stream, items, SerializerOptions, cancellationToken);
    }

    private string PairingsFile => Path.Combine(dataDirectories.ServerPairingsRoot, "pairings.json");

    private sealed record ServerPairingFileItem
    {
        public ServerPairingFileItem(
            Guid id,
            string clientName,
            string accessToken,
            DateTimeOffset createdAt,
            bool isRevoked,
            DateTimeOffset revokedAt)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Pairing identifier cannot be empty.", nameof(id));
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(clientName);
            ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

            Id = id;
            ClientName = clientName;
            AccessToken = accessToken;
            CreatedAt = createdAt;
            IsRevoked = isRevoked;
            RevokedAt = revokedAt;
        }

        public Guid Id { get; }

        public string ClientName { get; }

        public string AccessToken { get; }

        public DateTimeOffset CreatedAt { get; }

        public bool IsRevoked { get; }

        public DateTimeOffset RevokedAt { get; }

        public static ServerPairingFileItem FromPairing(ServerPairing pairing)
        {
            ArgumentNullException.ThrowIfNull(pairing);

            return new ServerPairingFileItem(
                pairing.Id,
                pairing.ClientName,
                pairing.AccessToken,
                pairing.CreatedAt,
                pairing.IsRevoked,
                pairing.RevokedAt);
        }

        public ServerPairing ToPairing() =>
            new(Id, ClientName, AccessToken, CreatedAt, IsRevoked, RevokedAt);
    }
}
