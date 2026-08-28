using System.Net.Http.Json;
using System.Text.Json;
using Zaphira.Client.Chat;
using Zaphira.Contracts;

namespace Zaphira.Client.ModelCatalog;

public sealed class HttpModelCatalogApiClient : IModelCatalogApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;

    public HttpModelCatalogApiClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        this.httpClient = httpClient;
    }

    public async Task<ModelCatalogResponse> GetCatalogAsync(
        string query,
        string purpose,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(purpose);

        string path = "/api/model-catalog/";
        List<string> queryParts = [];
        if (!string.IsNullOrWhiteSpace(query))
        {
            queryParts.Add($"query={Uri.EscapeDataString(query)}");
        }

        if (!string.IsNullOrWhiteSpace(purpose) && !purpose.Equals("Any", StringComparison.OrdinalIgnoreCase))
        {
            queryParts.Add($"purpose={Uri.EscapeDataString(purpose)}");
        }

        if (queryParts.Count > 0)
        {
            path += "?" + string.Join("&", queryParts);
        }

        using HttpResponseMessage response = await httpClient.GetAsync(path, cancellationToken);
        await ThrowIfErrorAsync(response, cancellationToken);

        return await ReadRequiredJsonAsync<ModelCatalogResponse>(response, cancellationToken);
    }

    public async Task<ModelCatalogResponse> SyncCatalogAsync(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await httpClient.PostAsync(
            "/api/model-catalog/sync",
            content: null,
            cancellationToken);
        await ThrowIfErrorAsync(response, cancellationToken);

        return await ReadRequiredJsonAsync<ModelCatalogResponse>(response, cancellationToken);
    }

    private static async Task<T> ReadRequiredJsonAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
        where T : class
    {
        T? body = await response.Content.ReadFromJsonAsync<T>(SerializerOptions, cancellationToken);

        return body ?? throw new InvalidOperationException("The API returned an empty response body.");
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
