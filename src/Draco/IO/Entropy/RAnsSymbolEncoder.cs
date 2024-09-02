using Draco.IO.Extensions;

namespace Draco.IO.Entropy;

internal class RAnsSymbolEncoder : ISymbolEncoder
{
    private int _rAnsPrecisionBits;
    private int _rAnsPrecision;
    private ulong _numExpectedBits;
    private readonly List<RAnsSymbol> _probabilityTable = [];
    private RAnsEncoder? _ans;
    public uint NumSymbols { get; private set; }
    public bool NeedsReverseEncoding { get => true; }

    public void Create(EncoderBuffer encoderBuffer, int maxBitLength, ulong[] frequencies, int numSymbols)
    {
        _rAnsPrecisionBits = RAnsSymbolCoding.ComputeRAnsPrecisionFromUniqueSymbolsBitLength(maxBitLength);
        _rAnsPrecision = 1 << _rAnsPrecisionBits;
        _ans = new RAnsEncoder(_rAnsPrecisionBits);
        ulong totalFrequency = 0;
        int maxValidSymbol = 0;

        for (int i = 0; i < numSymbols; ++i)
        {
            totalFrequency += frequencies[i];
            if (frequencies[i] > 0)
            {
                maxValidSymbol = i;
            }
        }
        NumSymbols = (uint)maxValidSymbol + 1;
        _probabilityTable.Fill((int)NumSymbols, () => new());
        double totalFrequencyAsDouble = totalFrequency;
        double rAnsPrecisionAsDouble = _rAnsPrecision;
        int totalRAnsProb = 0;
        for (int i = 0; i < NumSymbols; ++i)
        {
            var frequency = frequencies[i];
            var prob = Constants.StaticCast<ulong, double>(frequency) / totalFrequencyAsDouble;
            uint ransProb = Constants.StaticCast<double, uint>(prob * rAnsPrecisionAsDouble + 0.5f);
            if (ransProb == 0 && frequency > 0)
            {
                ransProb = 1;
            }
            _probabilityTable[i].Probability = ransProb;
            totalRAnsProb += (int)ransProb;
        }

        if (totalRAnsProb != _rAnsPrecision)
        {
            var sortedProbabilities = new List<int>((int)NumSymbols);
            for (int i = 0; i < NumSymbols; ++i)
            {
                sortedProbabilities.Add(i);
            }
            sortedProbabilities.Sort((a, b) => _probabilityTable[a].Probability.CompareTo(_probabilityTable[b].Probability));
            if (totalRAnsProb < _rAnsPrecision)
            {
                _probabilityTable[sortedProbabilities.Last()].Probability += (uint)(_rAnsPrecision - totalRAnsProb);
            }
            else
            {
                int error = totalRAnsProb - _rAnsPrecision;
                while (error > 0)
                {
                    double actTotalProbAsDouble = totalRAnsProb;
                    double actRelErrorAsDouble = rAnsPrecisionAsDouble / actTotalProbAsDouble;

                    for (int j = (int)NumSymbols - 1; j >= 0; --j)
                    {
                        int symbolId = sortedProbabilities[j];
                        if (_probabilityTable[symbolId].Probability <= 1)
                        {
                            Assertions.ThrowIf(j == NumSymbols - 1, "Most frequent symbol would be empty.");
                            break;
                        }
                        int newProb = (int)Math.Floor(actRelErrorAsDouble * _probabilityTable[symbolId].Probability);
                        int fix = (int)_probabilityTable[symbolId].Probability - newProb;
                        if (fix == 0U)
                        {
                            fix = 1;
                        }
                        if (fix >= _probabilityTable[symbolId].Probability)
                        {
                            fix = (int)(_probabilityTable[symbolId].Probability - 1);
                        }
                        if (fix > error)
                        {
                            fix = error;
                        }
                        _probabilityTable[symbolId].Probability -= (uint)fix;
                        totalRAnsProb -= fix;
                        error -= fix;
                        if (totalRAnsProb == _rAnsPrecision)
                        {
                            break;
                        }
                    }
                }
            }
        }

        uint totalProb = 0;
        for (int i = 0; i < NumSymbols; ++i)
        {
            _probabilityTable[i].CumulativeProbability = totalProb;
            totalProb += _probabilityTable[i].Probability;
        }
        Assertions.ThrowIf(totalProb != _rAnsPrecision, "Total probability must equal RAns precision");

        double numBits = 0;
        for (int i = 0; i < NumSymbols; ++i)
        {
            if (_probabilityTable[i].Probability == 0)
            {
                continue;
            }
            var normProb = _probabilityTable[i].Probability / (double)rAnsPrecisionAsDouble;
            numBits += Constants.StaticCast<ulong, double>(frequencies[i]) * Math.Log2(normProb);
        }
        _numExpectedBits = Constants.StaticCast<double, ulong>(Math.Ceiling(-numBits));
        EncodeTable(encoderBuffer);
    }

    private void EncodeTable(EncoderBuffer encoderBuffer)
    {
        encoderBuffer.EncodeVarIntUnsigned(NumSymbols);
        for (int i = 0; i < NumSymbols; ++i)
        {
            var prob = _probabilityTable[i].Probability;
            int numExtraBytes = 0;
            if (prob >= 65536)
            {
                numExtraBytes++;
                if (prob >= 16384)
                {
                    numExtraBytes++;
                    Assertions.ThrowIf(prob >= 4194304, "The maximum number of precision bits is 20 so we should not really get to this point.");
                }
            }
            if (prob == 0)
            {
                byte offset = 0;
                for (; offset < 63; ++offset)
                {
                    var nextProb = _probabilityTable[i + offset + 1].Probability;
                    if (nextProb > 0)
                    {
                        break;
                    }
                }
                encoderBuffer.WriteByte((byte)((offset << 2) | 3));
                i += offset;
            }
            else
            {
                encoderBuffer.WriteByte((byte)((prob << 2) | ((uint)numExtraBytes & 3)));
                for (int b = 0; b < numExtraBytes; ++b)
                {
                    encoderBuffer.WriteByte((byte)(prob >> (8 * (b + 1) - 2)));
                }
            }
        }
    }

    public void StartEncoding(EncoderBuffer encoderBuffer)
    {
        ulong requiredBits = 2 * _numExpectedBits + 32;
        long requiredBytes = (long)((requiredBits + 7) / 8);
        _ans!.WriteInit(new byte[requiredBytes]);
    }

    public void EncodeSymbol(uint symbol)
    {
        _ans!.Write(_probabilityTable[(int)symbol]);
    }

    public void EndEncoding(EncoderBuffer encoderBuffer)
    {
        var bytesWritten = _ans!.WriteEnd();
        encoderBuffer.EncodeVarIntUnsigned((ulong)bytesWritten);
        encoderBuffer.WriteBytes(_ans!.Buffer);
    }
}
