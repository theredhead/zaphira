using System.Net;
using Microsoft.AspNetCore.Http;
using Zaphira.Infrastructure.Storage;
using Zaphira.Server.Pairing;

namespace Zaphira.Server.Tests;

public sealed class PairingAuthorizationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsyncAllowsLoopbackRequestsWithoutPairingToken()
    {
        string homeDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        ZaphiraDataDirectories dataDirectories = ZaphiraDataDirectories.ForHomeDirectory(homeDirectory);
        await dataDirectories.EnsureServerDirectoriesExistAsync(CancellationToken.None);
        ServerPairingRegistry pairingRegistry = new(
            new ServerPairingStore(dataDirectories),
            TimeProvider.System);
        PairingCode pairingCode = pairingRegistry.CreatePairingCode();
        await pairingRegistry.CreatePairingAsync(pairingCode.Value, "Remote Client", CancellationToken.None);
        bool nextWasCalled = false;
        PairingAuthorizationMiddleware middleware = new(_ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        });
        DefaultHttpContext context = new();
        context.Request.Path = "/api/models";
        context.Connection.RemoteIpAddress = IPAddress.Loopback;

        await middleware.InvokeAsync(context, pairingRegistry);

        Assert.True(nextWasCalled);

        Directory.Delete(homeDirectory, recursive: true);
    }
}
