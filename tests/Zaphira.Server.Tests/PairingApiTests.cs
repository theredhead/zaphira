using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Zaphira.Contracts;

namespace Zaphira.Server.Tests;

public sealed class PairingApiTests
{
    [Fact]
    public async Task CreatePairingCodeReturnsFourDigitCode()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(homeDirectory);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage httpResponse = await client.PostAsJsonAsync("/api/pairing-code", new { });
        CreatePairingCodeResponse? response = await httpResponse.Content.ReadFromJsonAsync<CreatePairingCodeResponse>();

        Assert.NotNull(response);
        Assert.Matches("^[0-9]{4}$", response.Code);
        Assert.True(response.ExpiresAt > DateTimeOffset.UtcNow);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task CreatePairingRejectsInvalidCode()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(homeDirectory);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/pairings",
            new CreatePairingRequest("0000", "Test Client"));
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("pairing_code_invalid", error.Code);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task CreatePairingIssuesCredentialsAndPersistsPairing()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(homeDirectory);
        using HttpClient client = factory.CreateClient();
        CreatePairingCodeResponse code = await CreatePairingCodeAsync(client);

        using HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/api/pairings",
            new CreatePairingRequest(code.Code, "Test Client"));
        CreatePairingResponse? created = await createResponse.Content.ReadFromJsonAsync<CreatePairingResponse>();
        PairingListResponse? pairings = await client.GetFromJsonAsync<PairingListResponse>("/api/pairings");

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.PairingId);
        Assert.False(string.IsNullOrWhiteSpace(created.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(created.BackendCertificateThumbprint));
        Assert.NotNull(pairings);
        PairingResponse pairing = Assert.Single(pairings.Pairings);
        Assert.Equal(created.PairingId, pairing.Id);
        Assert.Equal("Test Client", pairing.ClientName);
        Assert.False(pairing.IsRevoked);
        Assert.Equal(PairingResponse.NotRevokedAt, pairing.RevokedAt);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task RevokePairingMarksPairingRevoked()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(homeDirectory);
        using HttpClient client = factory.CreateClient();
        CreatePairingCodeResponse code = await CreatePairingCodeAsync(client);
        CreatePairingResponse created = await CreatePairingAsync(client, code.Code);

        using HttpRequestMessage revokeRequest = new(HttpMethod.Delete, $"/api/pairings/{created.PairingId}");
        revokeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", created.AccessToken);

        using HttpResponseMessage revokeResponse = await client.SendAsync(revokeRequest);
        PairingListResponse? pairings = await client.GetFromJsonAsync<PairingListResponse>("/api/pairings");

        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);
        Assert.NotNull(pairings);
        PairingResponse revoked = Assert.Single(pairings.Pairings);
        Assert.True(revoked.IsRevoked);
        Assert.NotEqual(PairingResponse.NotRevokedAt, revoked.RevokedAt);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task RevokedPairingTokenDoesNotAuthorizeApiRequests()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(homeDirectory);
        using HttpClient client = factory.CreateClient();
        CreatePairingCodeResponse code = await CreatePairingCodeAsync(client);
        CreatePairingResponse created = await CreatePairingAsync(client, code.Code);
        using HttpRequestMessage revokeRequest = new(HttpMethod.Delete, $"/api/pairings/{created.PairingId}");
        revokeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", created.AccessToken);
        using HttpResponseMessage revokeResponse = await client.SendAsync(revokeRequest);
        using HttpRequestMessage authorizedRequest = new(HttpMethod.Get, "/api/models");
        authorizedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", created.AccessToken);

        using HttpResponseMessage response = await client.SendAsync(authorizedRequest);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("pairing_required", error.Code);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task PairingTokenAuthorizesApiRequestsAfterPairing()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(homeDirectory);
        using HttpClient client = factory.CreateClient();
        CreatePairingCodeResponse code = await CreatePairingCodeAsync(client);
        CreatePairingResponse created = await CreatePairingAsync(client, code.Code);
        using HttpRequestMessage request = new(HttpMethod.Get, "/api/models");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", created.AccessToken);

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task RevokePairingReturnsNotFoundForMissingPairing()
    {
        string homeDirectory = CreateTemporaryHomeDirectory();

        await using ZaphiraServerApplicationFactory factory = new(homeDirectory);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.DeleteAsync($"/api/pairings/{Guid.NewGuid()}");
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("pairing_not_found", error.Code);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    private static async Task<CreatePairingCodeResponse> CreatePairingCodeAsync(HttpClient client)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/pairing-code", new { });
        CreatePairingCodeResponse? code = await response.Content.ReadFromJsonAsync<CreatePairingCodeResponse>();

        return code ?? throw new InvalidOperationException("Pairing code response was missing.");
    }

    private static async Task<CreatePairingResponse> CreatePairingAsync(HttpClient client, string code)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/pairings",
            new CreatePairingRequest(code, "Test Client"));
        CreatePairingResponse? pairing = await response.Content.ReadFromJsonAsync<CreatePairingResponse>();

        return pairing ?? throw new InvalidOperationException("Pairing response was missing.");
    }

    private static string CreateTemporaryHomeDirectory() =>
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    private static void DeleteDirectoryIfItExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private sealed class ZaphiraServerApplicationFactory(string homeDirectory) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration(configurationBuilder =>
            {
                configurationBuilder.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Zaphira:HomeDirectory"] = homeDirectory
                    });
            });
        }
    }
}
