namespace MyrientDL.Data;

public record FileSize
{
    public required decimal Bytes { get; init; }

    public static FileSize FromByteString(string bytesString)
    {
        try
        {
            var bytesStringSplit = bytesString.Split(' ');
            
            if (bytesStringSplit.Length != 2)
                throw new ArgumentException("Invalid bytes string.");

            var bytesTuple = (decimal.Parse(bytesStringSplit[0]), Enum.Parse<FileSizeDenominations>(bytesStringSplit[1]));

            return new FileSize { Bytes = bytesTuple.Item1 * (ulong)bytesTuple.Item2 };
        }
        catch (Exception e)
        {
            Console.WriteLine(e);

            return new FileSize { Bytes = 0 };
        }
    }

    public override string ToString()
    {
        var truncatedBytes = Bytes;
        var fileSizeDenomination = FileSizeDenominations.Bytes;
        
        foreach (var denomination in Enum.GetValues<FileSizeDenominations>())
        {
            if (Bytes >= (ulong)denomination * 1024)
                continue;

            truncatedBytes /= (ulong)denomination;
            fileSizeDenomination = denomination;

            break;
        }
        
        return $"{truncatedBytes:0.00} {fileSizeDenomination}";
    }
}