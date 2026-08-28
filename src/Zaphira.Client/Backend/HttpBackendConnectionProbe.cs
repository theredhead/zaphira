using System.Net.Http.Json;
using System.Text.Json;
using Zaphira.Contracts;

namespace Zaphira.Client.Backend;

public sealed class HttpBackendConnectionProbe : IBackendConnectionProbe
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;

    public HttpBackendConnectionProbe(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        this.httpClient = httpClient;
    }

    public async Task<BackendConnectionProbeResult> CheckConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync("/health", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return BackendConnectionProbeResult.Unavailable;
            }

            HealthResponse? health = await response.Content.ReadFromJsonAsync<HealthResponse>(
                SerializerOptions,
                cancellationToken);

            return health is null
                ? BackendConnectionProbeResult.Unavailable
                : BackendConnectionProbeResult.Connected;
        }
        catch (Exception exception) when (exception is HttpRequestException
                                        || exception is TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return BackendConnectionProbeResult.Unavailable;
        }
    }
}
