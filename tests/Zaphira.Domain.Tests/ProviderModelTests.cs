using Zaphira.Domain;

namespace Zaphira.Domain.Tests;

public sealed class ProviderModelTests
{
    [Fact]
    public void ProviderIdRejectsEmptyValue()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new ProviderId(" "));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void ModelIdRejectsEmptyValue()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new ModelId(" "));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void ProviderCapabilitiesStoresDistinctCapabilities()
    {
        ProviderCapabilities capabilities = new(
            [ProviderCapability.TextGeneration, ProviderCapability.TextGeneration, ProviderCapability.FileInput]);

        Assert.Equal([ProviderCapability.TextGeneration, ProviderCapability.FileInput], capabilities.Values);
    }

    [Fact]
    public void ProviderCapabilitiesRejectsNullValues()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new ProviderCapabilities(null!));

        Assert.Equal("values", exception.ParamName);
    }
}
