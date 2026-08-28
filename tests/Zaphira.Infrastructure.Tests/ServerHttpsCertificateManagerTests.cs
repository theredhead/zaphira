using Zaphira.Infrastructure.Security;
using Zaphira.Infrastructure.Storage;

namespace Zaphira.Infrastructure.Tests;

public sealed class ServerHttpsCertificateManagerTests
{
    [Fact]
    public async Task LoadOrCreateAsyncCreatesCertificateFile()
    {
        string homeDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        ZaphiraDataDirectories directories = ZaphiraDataDirectories.ForHomeDirectory(homeDirectory);
        ServerHttpsCertificateManager manager = new();

        ServerHttpsCertificateMaterial material = await manager.LoadOrCreateAsync(directories, CancellationToken.None);

        Assert.True(File.Exists(directories.ServerHttpsCertificateFile));
        Assert.Equal(directories.ServerHttpsCertificateFile, material.CertificatePath);
        Assert.True(material.Certificate.HasPrivateKey);
        Assert.False(string.IsNullOrWhiteSpace(material.Thumbprint));
        Assert.Equal("Loaded HTTPS certificate for localhost.", material.DiagnosticMessage);

        Directory.Delete(homeDirectory, recursive: true);
    }

    [Fact]
    public async Task LoadOrCreateAsyncReusesExistingValidCertificate()
    {
        string homeDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        ZaphiraDataDirectories directories = ZaphiraDataDirectories.ForHomeDirectory(homeDirectory);
        ServerHttpsCertificateManager manager = new();

        ServerHttpsCertificateMaterial firstMaterial = await manager.LoadOrCreateAsync(directories, CancellationToken.None);
        ServerHttpsCertificateMaterial secondMaterial = await manager.LoadOrCreateAsync(directories, CancellationToken.None);

        Assert.Equal(firstMaterial.Thumbprint, secondMaterial.Thumbprint);

        Directory.Delete(homeDirectory, recursive: true);
    }
}
