using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Zaphira.Client.Chat;
using Zaphira.Contracts;

namespace Zaphira.Client.Pairing;

public sealed class HttpRemoteBackendPairingClient : IRemoteBackendPairingClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;

    public HttpRemoteBackendPairingClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        this.httpClient = httpClient;
    }

    public async Task<bool> CheckBackendAsync(CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync("/health", cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return false;
        }
    }

    public async Task<CreatePairingCodeResponse> CreatePairingCodeAsync(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "/api/pairing-code",
            new { },
            SerializerOptions,
            cancellationToken);
        await ThrowIfErrorAsync(response, cancellationToken);

        CreatePairingCodeResponse? pairingCode =
            await response.Content.ReadFromJsonAsync<CreatePairingCodeResponse>(SerializerOptions, cancellationToken);

        return pairingCode ?? throw new InvalidOperationException("Pairing code response was missing.");
    }

    public async Task<CreatePairingResponse> PairAsync(
        string pairingCode,
        string clientName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pairingCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);

        using HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "/api/pairings",
            new CreatePairingRequest(pairingCode, clientName),
            SerializerOptions,
            cancellationToken);
        await ThrowIfErrorAsync(response, cancellationToken);

        CreatePairingResponse? pairingResponse =
            await response.Content.ReadFromJsonAsync<CreatePairingResponse>(SerializerOptions, cancellationToken);

        return pairingResponse ?? throw new InvalidOperationException("Pairing response was missing.");
    }

    public async Task RevokePairingAsync(Guid pairingId, string accessToken, CancellationToken cancellationToken)
    {
        if (pairingId == Guid.Empty)
        {
            throw new ArgumentException("Pairing identifier cannot be empty.", nameof(pairingId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        using HttpRequestMessage request = new(HttpMethod.Delete, $"/api/pairings/{pairingId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        await ThrowIfErrorAsync(response, cancellationToken);
    }

    private static async Task ThrowIfErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(SerializerOptions, cancellationToken);
        ErrorResponse nonNullError = error ?? ErrorResponse.UnexpectedServerError();

        throw new ChatApiException((int)response.StatusCode, nonNullError);
    }
}
