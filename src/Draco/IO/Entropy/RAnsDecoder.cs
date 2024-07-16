using Draco.IO.Extensions;

namespace Draco.IO.Entropy;

internal class RAnsDecoder : AnsDecoder
{
    private readonly int _rAnsPrecisionBits;
    private readonly int _rAnsPrecision;
    private readonly int _lRAnsBase = Constants.LRAnsBase;
    private readonly List<uint> _lutTable = [];
    private readonly List<RAnsSymbol> _probabilityTable = [];

    public RAnsDecoder(int rAnsPrecisionBits)
    {
        _rAnsPrecisionBits = rAnsPrecisionBits;
        _rAnsPrecision = 1 << rAnsPrecisionBits;
        _lRAnsBase = _rAnsPrecision * 4;
    }

    public override void ReadInit(byte[] buffer, int offset)
    {
        Assertions.ThrowIf(offset < 1, "Offset must be greater than or equal to 1");
        Buffer = buffer;
        uint x = (uint)buffer[offset - 1] >> 6;

        if (x == 0)
        {
            BufferOffset = offset - 1;
            State = (uint)buffer[offset - 2] & 0x3f;
        }
        else if (x == 1)
        {
            Assertions.ThrowIf(offset < 2, "Offset must be greater than or equal to 2");
            BufferOffset = offset - 2;
            State = Ans.MemGetLE16([buffer[offset - 2], buffer[offset - 1]]) & 0x3FFF;
        }
        else if (x == 2)
        {
            Assertions.ThrowIf(offset < 3, "Offset must be greater than or equal to 3");
            BufferOffset = offset - 3;
            State = Ans.MemGetLE24([buffer[offset - 3], buffer[offset - 2], buffer[offset - 1]]) & 0x3FFFFF;
        }
        else if (x == 3)
        {
            BufferOffset = offset - 4;
            State = Ans.MemGetLE32([buffer[offset - 4], buffer[offset - 3], buffer[offset - 2], buffer[offset - 1]]) & 0x3FFFFFFF;
        }
        else
        {
            Assertions.Throw("Invalid data");
        }
        State += (uint)_lRAnsBase;
        Assertions.ThrowIf(State >= (uint)_lRAnsBase * Constants.AnsIOBase, "Invalid state");
    }

    public uint Read()
    {
        while (State < _lRAnsBase && BufferOffset > 0)
        {
            State = State * Constants.AnsIOBase + Buffer[--BufferOffset];
        }
        uint quo = (uint)(State / _rAnsPrecision);
        uint rem = (uint)(State % _rAnsPrecision);
        var symbol = FetchSymbol(rem);
        State = quo * symbol.Probability + rem - symbol.CumulativeProbability;
        return symbol.Value;
    }

    public void BuildLookupTable(IList<uint> tokenProbabilities, uint numSymbols)
    {
        _lutTable.Fill((int)_rAnsPrecision, 0U);
        _probabilityTable.Fill((int)numSymbols, () => new RAnsSymbol());
        uint cumProb = 0;
        uint actProb = 0;
        for (uint i = 0; i < numSymbols; ++i)
        {
            _probabilityTable[(int)i].Probability = tokenProbabilities[(int)i];
            _probabilityTable[(int)i].CumulativeProbability = cumProb;
            cumProb += tokenProbabilities[(int)i];
            Assertions.ThrowIf(cumProb > _rAnsPrecision, "Invalid probability table");
            for (uint j = actProb; j < cumProb; ++j)
            {
                _lutTable[(int)j] = i;
            }
            actProb = cumProb;
        }
        Assertions.ThrowIf(cumProb != _rAnsPrecision, "Invalid probability table");
    }

    private RAnsDecodedSymbol FetchSymbol(uint rem)
    {
        var symbol = _lutTable[(int)rem];
        return new()
        {
            Value = symbol,
            Probability = _probabilityTable[(int)symbol].Probability,
            CumulativeProbability = _probabilityTable[(int)symbol].CumulativeProbability
        };
    }
}
