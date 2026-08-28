using Zaphira.Domain;

namespace Zaphira.Domain.Tests;

public sealed class FileMessagePartTests
{
    [Fact]
    public void ConstructorStoresFileReference()
    {
        FileReference reference = new(
            "notes.txt",
            "text/plain",
            "files/conversations/notes.txt",
            42);

        FileMessagePart part = new(reference);

        Assert.Equal(reference, part.Reference);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void FileReferenceRejectsEmptyDisplayName(string displayName)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new FileReference(displayName, "text/plain", "files/notes.txt", 42));

        Assert.Equal("displayName", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void FileReferenceRejectsEmptyMediaType(string mediaType)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new FileReference("notes.txt", mediaType, "files/notes.txt", 42));

        Assert.Equal("mediaType", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void FileReferenceRejectsEmptyStorageLocation(string storageLocation)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new FileReference("notes.txt", "text/plain", storageLocation, 42));

        Assert.Equal("storageLocation", exception.ParamName);
    }

    [Fact]
    public void FileReferenceRejectsNegativeSize()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FileReference("notes.txt", "text/plain", "files/notes.txt", -1));

        Assert.Equal("sizeInBytes", exception.ParamName);
    }
}
