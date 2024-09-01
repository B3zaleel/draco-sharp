namespace Draco.IO.Entropy;

/// <summary>
/// Represents an object that holds entropy data about the symbols added to the entropy tracker.
/// </summary>
internal class EntropyData
{
    public double EntropyNorm { get; set; }
    public int ValuesCount { get; set; }
    public int MaxSymbol { get; set; }
    public int UniqueSymbolsCount { get; set; }
}
