namespace MyrientDL.Data;

public enum FileSizeDenominations : ulong
{
    Bytes = 1,
    B = Bytes,
    
    Kibibytes = 1024,
    KiB = Kibibytes,
    
    Mebibytes = Kibibytes * 1024,
    MiB = Mebibytes,
    
    Gibibytes = Mebibytes * 1024,
    GiB = Gibibytes,
    
    Tebibytes = Gibibytes * 1024,
    TiB = Tebibytes
}