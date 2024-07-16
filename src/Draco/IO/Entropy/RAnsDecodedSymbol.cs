namespace Draco.IO.Entropy;

public class RAnsDecodedSymbol
{
    public uint Value { get; set; }
    public uint Probability { get; set; }
    public uint CumulativeProbability { get; set; }
}
