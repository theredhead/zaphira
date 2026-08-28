using System.Text.Json;
using Zaphira.Client.Storage;

namespace Zaphira.Client.Security;

public sealed class TrustedBackendConnectionStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly ZaphiraClientDataDirectories dataDirectories;

    public TrustedBackendConnectionStore(ZaphiraClientDataDirectories dataDirectories)
    {
        ArgumentNullException.ThrowIfNull(dataDirectories);

        this.dataDirectories = dataDirectories;
    }

    public async Task<IReadOnlyList<TrustedBackendConnection>> LoadAsync(CancellationToken cancellationToken)
    {
        await dataDirectories.EnsureClientDirectoriesExistAsync(cancellationToken);

        if (!File.Exists(dataDirectories.ConnectionsFile))
        {
            return [];
        }

        await using FileStream stream = File.OpenRead(dataDirectories.ConnectionsFile);
        List<TrustedBackendConnectionFileItem>? items =
            await JsonSerializer.DeserializeAsync<List<TrustedBackendConnectionFileItem>>(stream, SerializerOptions, cancellationToken);

        if (items is null)
        {
            throw new InvalidOperationException("Trusted backend connection file did not contain connection records.");
        }

        return items.Select(item => item.ToConnection()).ToArray();
    }

    public async Task SaveAsync(TrustedBackendConnection connection, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);

        List<TrustedBackendConnection> connections = [.. await LoadAsync(cancellationToken)];
        int existingConnectionIndex = connections.FindIndex(existingConnection =>
            Uri.Compare(
                existingConnection.BackendAddress,
                connection.BackendAddress,
                UriComponents.HttpRequestUrl,
                UriFormat.SafeUnescaped,
                StringComparison.OrdinalIgnoreCase) == 0);

        if (existingConnectionIndex >= 0)
        {
            connections[existingConnectionIndex] = connection;
        }
        else
        {
            connections.Add(connection);
        }

        TrustedBackendConnectionFileItem[] items = connections
            .Select(TrustedBackendConnectionFileItem.FromConnection)
            .ToArray();
        await using FileStream stream = File.Create(dataDirectories.ConnectionsFile);

        await JsonSerializer.SerializeAsync(stream, items, SerializerOptions, cancellationToken);
    }

    public async Task RemoveAsync(Uri backendAddress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(backendAddress);

        List<TrustedBackendConnection> connections = [.. await LoadAsync(cancellationToken)];
        connections.RemoveAll(connection =>
            Uri.Compare(
                connection.BackendAddress,
                backendAddress,
                UriComponents.HttpRequestUrl,
                UriFormat.SafeUnescaped,
                StringComparison.OrdinalIgnoreCase) == 0);

        TrustedBackendConnectionFileItem[] items = connections
            .Select(TrustedBackendConnectionFileItem.FromConnection)
            .ToArray();
        await using FileStream stream = File.Create(dataDirectories.ConnectionsFile);

        await JsonSerializer.SerializeAsync(stream, items, SerializerOptions, cancellationToken);
    }

    private sealed record TrustedBackendConnectionFileItem
    {
        public TrustedBackendConnectionFileItem(
            string backendAddress,
            string certificateThumbprint,
            string description,
            Guid pairingId,
            string accessToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(backendAddress);
            ArgumentException.ThrowIfNullOrWhiteSpace(certificateThumbprint);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);
            ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

            BackendAddress = backendAddress;
            CertificateThumbprint = certificateThumbprint;
            Description = description;
            PairingId = pairingId;
            AccessToken = accessToken;
        }

        public string BackendAddress { get; }

        public string CertificateThumbprint { get; }

        public string Description { get; }

        public Guid PairingId { get; }

        public string AccessToken { get; }

        public static TrustedBackendConnectionFileItem FromConnection(TrustedBackendConnection connection)
        {
            ArgumentNullException.ThrowIfNull(connection);

            return new TrustedBackendConnectionFileItem(
                connection.BackendAddress.ToString(),
                connection.CertificateThumbprint,
                connection.Description,
                connection.PairingId,
                connection.AccessToken);
        }

        public TrustedBackendConnection ToConnection() =>
            new(new Uri(BackendAddress), CertificateThumbprint, Description, PairingId, AccessToken);
    }
}
