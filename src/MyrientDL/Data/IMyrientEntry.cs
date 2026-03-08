using System.Text.Json.Serialization;

namespace MyrientDL.Data;

[JsonDerivedType(typeof(MyrientDirectory), nameof(MyrientDirectory))]
[JsonDerivedType(typeof(MyrientFile), nameof(MyrientFile))]
public interface IMyrientEntry
{
    public string Name { get; init; }
    public Uri Path { get; init; }
    public FileSize Size { get; }
    public bool IsDownloaded { get; set; }
}