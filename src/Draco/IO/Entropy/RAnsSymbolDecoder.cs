using Draco.IO.Extensions;

namespace Draco.IO.Entropy;

internal class RAnsSymbolDecoder : ISymbolDecoder
{
    private int _rAnsPrecisionBits;
    private readonly List<uint> _probabilityTable = [];
    private RAnsDecoder? _ans;
    public uint NumSymbols { get; private set; }

    public void Create(DecoderBuffer decoderBuffer, int maxBitLength)
    {
        _rAnsPrecisionBits = RAnsSymbolCoding.ComputeRAnsPrecisionFromUniqueSymbolsBitLength(maxBitLength);

        if (decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 0))
        {
            NumSymbols = decoderBuffer.ReadUInt32();
        }
        else
        {
            NumSymbols = (uint)decoderBuffer.DecodeVarIntUnsigned();
        }
        _probabilityTable.Fill((int)NumSymbols, 0U);
        if (NumSymbols == 0)
        {
            return;
        }
        for (uint i = 0; i < NumSymbols; ++i)
        {
            var probData = decoderBuffer.ReadByte();
            var token = probData & 3;
            if (token == 3)
            {
                uint offset = (uint)probData >> 2;
                Assertions.ThrowIf(i + offset >= NumSymbols);
                for (uint j = 0; j < offset + 1; ++j)
                {
                    _probabilityTable[(int)(i + j)] = 0;
                }
                i += offset;
            }
            else
            {
                uint prob = (uint)probData >> 2;
                for (int b = 0; b < token; ++b)
                {
                    var eb = decoderBuffer.ReadByte();
                    prob |= (uint)(eb << (8 * (b + 1) - 2));
                }
                _probabilityTable[(int)i] = prob;
            }
        }
        _ans = new(_rAnsPrecisionBits);
        _ans.BuildLookupTable(_probabilityTable, NumSymbols);
    }

    public void StartDecoding(DecoderBuffer decoderBuffer)
    {
        ulong bytesEncoded = decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 0)
            ? decoderBuffer.ReadUInt64()
            : decoderBuffer.DecodeVarIntUnsigned();
        _ans!.ReadInit(decoderBuffer.ReadBytes((int)bytesEncoded), (int)bytesEncoded);
    }

    public uint DecodeSymbol()
    {
        return _ans!.Read();
    }

    public void EndDecoding()
    {
        _ans!.ReadEnd();
    }
}
