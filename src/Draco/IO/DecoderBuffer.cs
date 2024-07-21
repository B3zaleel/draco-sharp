using System.Text;
using Draco.IO.Extensions;

namespace Draco.IO;

internal sealed class DecoderBuffer : IDisposable
{
    private bool _bitMode = false;
    private byte _bitBuffer = 0;
    private byte _bitBufferIndex = 0;
    private readonly BinaryReader _binaryReader;

    public ushort BitStream_Version { get; set; }

    public DecoderBuffer(BinaryReader binaryReader)
    {
        _binaryReader = binaryReader;
    }

    public DecoderBuffer(byte[] data, ushort bitStreamVersion)
    {
        _binaryReader = new BinaryReader(new MemoryStream(data));
        BitStream_Version = bitStreamVersion;
    }

    public ulong DecodeVarIntUnsigned()
    {
        ulong result = 0;
        byte shift = 0;
        byte b;
        while (true)
        {
            b = _binaryReader.ReadByte();
            result |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                break;
            }
            shift += 7;
        }
        return result;
    }

    public long DecodeVarInt()
    {
        Assertions.ThrowIf(_bitMode, "Cannot execute this whilst bit mode is on");
        var symbol = DecodeVarIntUnsigned();
        return BitUtilities.ConvertSymbolToSignedInt(symbol);
    }

    public byte ReadByte()
    {
        Assertions.ThrowIf(_bitMode, "Cannot execute this whilst bit mode is on");
        return _binaryReader.ReadByte();
    }

    public ushort ReadUInt16()
    {
        Assertions.ThrowIf(_bitMode, "Cannot execute this whilst bit mode is on");
        return _binaryReader.ReadUInt16();
    }

    public uint ReadUInt32()
    {
        Assertions.ThrowIf(_bitMode, "Cannot execute this whilst bit mode is on");
        return _binaryReader.ReadUInt32();
    }

    public ulong ReadUInt64()
    {
        Assertions.ThrowIf(_bitMode, "Cannot execute this whilst bit mode is on");
        return _binaryReader.ReadUInt64();
    }

    public sbyte ReadSByte()
    {
        Assertions.ThrowIf(_bitMode, "Cannot execute this whilst bit mode is on");
        return _binaryReader.ReadSByte();
    }

    public short ReadInt16()
    {
        Assertions.ThrowIf(_bitMode, "Cannot execute this whilst bit mode is on");
        return _binaryReader.ReadInt16();
    }

    public int ReadInt32()
    {
        Assertions.ThrowIf(_bitMode, "Cannot execute this whilst bit mode is on");
        return _binaryReader.ReadInt32();
    }

    public float ReadSingle()
    {
        Assertions.ThrowIf(_bitMode, "Cannot execute this whilst bit mode is on");
        return _binaryReader.ReadSingle();
    }

    public string ReadASCIIBytes(int count)
    {
        Assertions.ThrowIf(_bitMode, "Cannot execute this whilst bit mode is on");
        return Encoding.ASCII.GetString(_binaryReader.ReadBytes(count));
    }

    public byte[] ReadBytes(int count)
    {
        Assertions.ThrowIf(_bitMode, "Cannot execute this whilst bit mode is on");
        return _binaryReader.ReadBytes(count);
    }

    public sbyte[] ReadSBytes(int count)
    {
        Assertions.ThrowIf(_bitMode, "Cannot execute this whilst bit mode is on");
        sbyte[] values = new sbyte[count];
        for (int i = 0; i < count; i++)
        {
            values[i] = _binaryReader.ReadSByte();
        }
        return values;
    }

    public uint DecodeLeastSignificantBits32(byte count)
    {
        Assertions.ThrowIfNot(_bitMode, "Cannot execute this whilst bit mode is not on");
        uint value = 0;

        for (byte i = 0; i < count; i++)
        {
            if (_bitBufferIndex >= 8)
            {
                _bitBuffer = _binaryReader.ReadByte();
                _bitBufferIndex = 0;
            }
            value |= (byte)(((_bitBuffer >> _bitBufferIndex) & 1) << i);
            _bitBufferIndex++;
        }
        return value;
    }

    public void StartBitDecoding(bool decodeSize, out ulong size)
    {
        size = 0;
        if (decodeSize)
        {
            size = BitStream_Version < Constants.BitStreamVersion(2, 2) ? _binaryReader.ReadUInt32() : DecodeVarIntUnsigned();
        }
        _bitMode = true;
        _bitBuffer = _binaryReader.ReadByte();
        _bitBufferIndex = 0;
    }

    public void EndBitDecoding()
    {
        _bitMode = false;
    }

    public void Dispose()
    {
        _binaryReader.Dispose();
    }
}
