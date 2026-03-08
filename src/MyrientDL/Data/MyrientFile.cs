namespace MyrientDL.Data;

public record MyrientFile : IMyrientEntry
{
    public required string Name { get; init; }
    public required Uri Path { get; init; }
    public required FileSize Size { get; init; }
    public bool IsDownloaded { get; set; }
}