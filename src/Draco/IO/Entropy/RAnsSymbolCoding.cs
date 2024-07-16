namespace Draco.IO.Entropy;

internal static class RAnsSymbolCoding
{
    /// <summary>
    /// Computes the desired precision of the rANS method for the specified number of unique symbols the input data (defined by their bit_length).
    /// </summary>
    /// <param name="symbolsBitLength"></param>
    /// <returns></returns>
    public static int ComputeRAnsUnclampedPrecision(int symbolsBitLength)
    {
        return (3 * symbolsBitLength) / 2;
    }

    /// <summary>
    /// Computes the desired precision clamped to guarantee a valid functionality of our rANS library (which is between 12 to 20 bits).
    /// </summary>
    /// <param name="symbolsBitLength"></param>
    /// <returns></returns>
    public static int ComputeRAnsPrecisionFromUniqueSymbolsBitLength(int symbolsBitLength)
    {
        return ComputeRAnsUnclampedPrecision(symbolsBitLength) < 12
                ? 12
                : ComputeRAnsUnclampedPrecision(symbolsBitLength) > 20
                   ? 20
                   : ComputeRAnsUnclampedPrecision(symbolsBitLength);
    }

    /// <summary>
    /// Compute approximate frequency table size needed for storing the provided symbols.
    /// </summary>
    /// <param name="maxValue"></param>
    /// <param name="numUniqueSymbols"></param>
    /// <returns></returns>
    public static long ApproximateRAnsFrequencyTableBits(int maxValue, int numUniqueSymbols)
    {
        // Approximate number of bits for storing zero frequency entries using the
        // run length encoding (with max length of 64).
        long tableZeroFrequencyBits = 8 * (numUniqueSymbols + (maxValue - numUniqueSymbols) / 64);
        return 8 * numUniqueSymbols + tableZeroFrequencyBits;
    }
}
