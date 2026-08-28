using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Zaphira.Contracts;

namespace Zaphira.Server.Tests;

public sealed class ServerHealthTests
{
    [Fact]
    public async Task HealthEndpointReturnsHealthyServerResponse()
    {
        string homeDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        await using ZaphiraServerApplicationFactory factory = new(homeDirectory);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        HealthResponse? response = await client.GetFromJsonAsync<HealthResponse>("/health");

        Assert.NotNull(response);
        Assert.Equal("Zaphira.Server", response.ServiceName);
        Assert.Equal("Healthy", response.Status);

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task StartupCreatesConfiguredServerDataDirectory()
    {
        string homeDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        await using ZaphiraServerApplicationFactory factory = new(homeDirectory);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using HttpResponseMessage response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(Directory.Exists(Path.Combine(homeDirectory, ".zaphira", "server")));

        DeleteDirectoryIfItExists(homeDirectory);
    }

    [Fact]
    public async Task UnknownRouteReturnsErrorResponse()
    {
        string homeDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        await using ZaphiraServerApplicationFactory factory = new(homeDirectory);
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using HttpResponseMessage response = await client.GetAsync("/unknown");
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal("route_not_found", error.Code);
        Assert.Equal("No endpoint matches the request.", error.Message);
        Assert.Equal("Check the endpoint path and HTTP method.", error.Suggestion);

        DeleteDirectoryIfItExists(homeDirectory);
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

    private static void DeleteDirectoryIfItExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
