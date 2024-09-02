namespace Draco.IO.Entropy;

internal static class ShannonEntropy
{
    public static (long Bits, int UniqueSymbolsCount) ComputeShannonEntropy(uint[] symbols, int maxValue)
    {
        int numUniqueSymbols = 0;
        var symbolFrequencies = new int[maxValue + 1];
        for (int i = 0; i < symbols.Length; ++i)
        {
            ++symbolFrequencies[symbols[i]];
        }
        double totalBits = 0;
        double numSymbolsAsDouble = symbols.Length;

        for (int i = 0; i < maxValue + 1; ++i)
        {
            if (symbolFrequencies[i] > 0)
            {
                ++numUniqueSymbols;
                totalBits += symbolFrequencies[i] * Math.Log2(symbolFrequencies[i] / numSymbolsAsDouble);
            }
        }
        return (Constants.StaticCast<double, long>(-totalBits), numUniqueSymbols);
    }

    public static double ComputeBinaryShannonEntropy(uint numValues, uint numTrueValues)
    {
        if (numValues == 0 || numTrueValues == 0 || numValues == numTrueValues)
        {
            return 0;
        }
        var trueFrequency = numTrueValues / numValues;
        var falseFrequency = 1 - trueFrequency;
        return -(trueFrequency * Math.Log2(trueFrequency) + falseFrequency * Math.Log2(falseFrequency));
    }

    public static long GetNumberOfDataBits(EntropyData entropyData)
    {
        return entropyData.ValuesCount < 2
            ? 0
            : (long)Math.Ceiling(entropyData.ValuesCount * Math.Log2(entropyData.ValuesCount) - entropyData.EntropyNorm);
    }

    public static long GetNumberOfRAnsTableBits(EntropyData entropyData)
    {
        return RAnsSymbolCoding.ApproximateRAnsFrequencyTableBits(entropyData.MaxSymbol + 1, entropyData.UniqueSymbolsCount);
    }
}
