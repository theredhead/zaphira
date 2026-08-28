namespace Zaphira.Domain;

public enum ProviderCapability
{
    TextGeneration = 0,
    StreamingGeneration = 1,
    FileInput = 2,
    ImageInput = 3,
    ToolUse = 4
}
