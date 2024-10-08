using Draco.IO.Entropy;
using Draco.IO.Extensions;

namespace Draco.IO.BitCoders;

internal class RAnsBitDecoder
{
    private byte _probZero = 0;
    private AnsDecoder _ansDecoder = new();
    private DecoderBuffer? _decoderBuffer = null;

    public void StartDecoding(DecoderBuffer decoderBuffer)
    {
        _decoderBuffer = decoderBuffer;
        _probZero = decoderBuffer.ReadByte();
        _ansDecoder = new();
        uint sizeInBytes = decoderBuffer.BitStreamVersion < Constants.BitStreamVersion(2, 2) ? decoderBuffer.ReadUInt32() : (uint)decoderBuffer.DecodeVarIntUnsigned();
        _ansDecoder.ReadInit(decoderBuffer.ReadBytes((int)sizeInBytes), (int)sizeInBytes);
    }

    public uint DecodeNextBit()
    {
        return _ansDecoder.RAbsRead(_probZero) ? 1U : 0U;
    }

    public uint DecodeLeastSignificantBits32(byte count)
    {
        Assertions.ThrowIfNot(count > 0, "Count must be greater than 0");
        Assertions.ThrowIfNot(count <= 32, "Count must be less than or equal to 32");
        uint value = 0;
        while (count > 0)
        {
            value = (value << 1) + DecodeNextBit();
            count--;
        }
        return value;
    }

    public void EndDecoding()
    {
        _decoderBuffer?.EndBitDecoding();
    }

    public void EndDecoding(DecoderBuffer decoderBuffer)
    {
        decoderBuffer.EndBitDecoding();
    }
}
