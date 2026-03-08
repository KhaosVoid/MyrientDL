namespace MyrientDL.Data;

public record MyrientDirectory : IMyrientEntry
{
    public required string Name { get; init; }
    public required Uri Path { get; init; }
    public required FileSize Size { get; init; }
    public bool IsDownloaded { get; set; }
    public required List<IMyrientEntry> Entries { get; init; }
}