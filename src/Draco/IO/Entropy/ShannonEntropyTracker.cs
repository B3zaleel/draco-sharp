using Draco.IO.Extensions;

namespace Draco.IO.Entropy;

internal class ShannonEntropyTracker
{
    private readonly List<int> _frequencies = [];
    private EntropyData _entropyData = new();

    public EntropyData Peek(uint[] symbols, int symbolsCount)
    {
        return UpdateSymbols(symbols, symbolsCount, false);
    }

    public EntropyData Push(uint[] symbols, int symbolsCount)
    {
        return UpdateSymbols(symbols, symbolsCount, true);
    }

    public long GetNumberOfDataBits()
    {
        return ShannonEntropy.GetNumberOfDataBits(_entropyData);
    }

    public long GetNumberOfRAnsTableBits()
    {
        return ShannonEntropy.GetNumberOfRAnsTableBits(_entropyData);
    }

    private EntropyData UpdateSymbols(uint[] symbols, int symbolsCount, bool pushChanges)
    {
        EntropyData data = new()
        {
            EntropyNorm = _entropyData.EntropyNorm,
            ValuesCount = _entropyData.ValuesCount + symbolsCount,
            MaxSymbol = _entropyData.MaxSymbol,
            UniqueSymbolsCount = _entropyData.UniqueSymbolsCount,
        };

        for (int i = 0; i < symbolsCount; ++i)
        {
            var symbol = symbols[i];
            if (_frequencies.Count <= symbol)
            {
                _frequencies.Resize((int)symbol + 1, 0);
            }
            double oldSymbolEntropyNorm = 0;
            int frequency = _frequencies[(int)symbol];
            if (frequency > 1)
            {
                oldSymbolEntropyNorm = frequency * Math.Log2(frequency);
            }
            else if (frequency == 0)
            {
                data.UniqueSymbolsCount++;
                if (symbol > data.MaxSymbol)
                {
                    data.MaxSymbol = (int)symbol;
                }
            }
            _frequencies[(int)symbol]++;
            data.EntropyNorm += frequency * Math.Log2(frequency) - oldSymbolEntropyNorm;
        }
        if (pushChanges)
        {
            _entropyData = data;
        }
        else
        {
            for (int i = 0; i < symbolsCount; ++i)
            {
                _frequencies[(int)symbols[i]]--;
            }
        }
        return data;
    }
}
