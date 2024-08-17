using System.Text;
using Draco.IO.Extensions;

namespace Draco.IO;

internal sealed class EncoderBuffer : IDisposable
{
    private bool _bitMode = false;
    private byte _bitBuffer = 0;
    private byte _bitBufferIndex = 0;
    private readonly BinaryWriter _binaryWriter;

    public ushort BitStreamVersion { get; set; }

    public EncoderBuffer(BinaryWriter binaryWriter)
    {
        _binaryWriter = binaryWriter;
    }

    public EncoderBuffer(byte[] data, ushort bitStreamVersion)
    {
        _binaryWriter = new BinaryWriter(new MemoryStream(data));
        BitStreamVersion = bitStreamVersion;
    }

    public void EncodeVarIntUnsigned(ulong value)
    {
        Assertions.ThrowIf(_bitMode, Constants.NonBitOperationDisallowedMessage);
        byte b = 0;

        while (true)
        {
            b = (byte)(value & 0x7F);

            if (value >= 0x80)
            {
                b |= 0x80;
            }
            _binaryWriter.Write(b);
            if (value < 0x80)
            {
                break;
            }
            value >>= 7;
        }
    }

    public void EncodeVarInt(long value)
    {
        Assertions.ThrowIf(_bitMode, Constants.NonBitOperationDisallowedMessage);
        EncodeVarIntUnsigned(BitUtilities.ConvertSignedIntToSymbol(value));
    }

    public void WriteByte(byte value)
    {
        Assertions.ThrowIf(_bitMode, Constants.NonBitOperationDisallowedMessage);
        _binaryWriter.Write(value);
    }

    public void WriteUInt16(ushort value)
    {
        Assertions.ThrowIf(_bitMode, Constants.NonBitOperationDisallowedMessage);
        _binaryWriter.Write(value);
    }

    public void WriteUInt32(uint value)
    {
        Assertions.ThrowIf(_bitMode, Constants.NonBitOperationDisallowedMessage);
        _binaryWriter.Write(value);
    }

    public void WriteUInt64(ulong value)
    {
        Assertions.ThrowIf(_bitMode, Constants.NonBitOperationDisallowedMessage);
        _binaryWriter.Write(value);
    }

    public void WriteSByte(sbyte value)
    {
        Assertions.ThrowIf(_bitMode, Constants.NonBitOperationDisallowedMessage);
        _binaryWriter.Write(value);
    }

    public void WriteInt16(short value)
    {
        Assertions.ThrowIf(_bitMode, Constants.NonBitOperationDisallowedMessage);
        _binaryWriter.Write(value);
    }

    public void WriteInt32(int value)
    {
        Assertions.ThrowIf(_bitMode, Constants.NonBitOperationDisallowedMessage);
        _binaryWriter.Write(value);
    }

    public void WriteSingle(float value)
    {
        Assertions.ThrowIf(_bitMode, Constants.NonBitOperationDisallowedMessage);
        _binaryWriter.Write(value);
    }

    public void WriteASCIIBytes(string text)
    {
        Assertions.ThrowIf(_bitMode, Constants.NonBitOperationDisallowedMessage);
        _binaryWriter.Write(Encoding.ASCII.GetBytes(text));
    }

    public void WriteBytes(byte[] value)
    {
        Assertions.ThrowIf(_bitMode, Constants.NonBitOperationDisallowedMessage);
        _binaryWriter.Write(value);
    }

    public void WriteSBytes(sbyte[] value)
    {
        Assertions.ThrowIf(_bitMode, Constants.NonBitOperationDisallowedMessage);
        for (int i = 0; i < value.Length; i++)
        {
            _binaryWriter.Write(value[i]);
        }
    }

    public void Write<T>(T value) where T : struct
    {
        if (value is byte dataAsByte)
        {
            WriteByte(dataAsByte);
        }
        else if (value is ushort dataAsUInt16)
        {
            WriteUInt16(dataAsUInt16);
        }
        else if (value is uint dataAsUInt32)
        {
            WriteUInt32(dataAsUInt32);
        }
        else if (value is ulong dataAsUInt64)
        {
            WriteUInt64(dataAsUInt64);
        }
        else if (value is sbyte dataAsSByte)
        {
            WriteSByte(dataAsSByte);
        }
        else if (value is short dataAsInt16)
        {
            WriteInt16(dataAsInt16);
        }
        else if (value is int dataAsInt32)
        {
            WriteInt32(dataAsInt32);
        }
        else if (value is float dataAsSingle)
        {
            WriteSingle(dataAsSingle);
        }
        else
        {
            throw new NotSupportedException($"Type {typeof(T).Name} is not supported");
        }
    }

    public void EncodeLeastSignificantBits32(byte count, uint value)
    {
        Assertions.ThrowIfNot(_bitMode, Constants.BitOperationDisallowedMessage);
        Assertions.ThrowIf(count > 32, "Count cannot be greater than 32");

        for (byte i = 0; i < count; i++)
        {
            if (_bitBufferIndex >= 8)
            {
                ReloadBitBuffer();
            }
            _bitBuffer |= (byte)(((value >> i) & 1) << _bitBufferIndex);
            _bitBufferIndex++;
        }
    }

    public void StartBitEncoding()
    {
        StartBitEncoding(false, 0);
    }

    public void StartBitEncoding(bool encodeSize, ulong size)
    {
        if (encodeSize)
        {
            if (BitStreamVersion < Constants.BitStreamVersion(2, 2))
            {
                _binaryWriter.Write((uint)size);
            }
            else
            {
                EncodeVarIntUnsigned(size);
            }
        }
        _bitMode = true;
        _bitBuffer = 0;
        _bitBufferIndex = 0;
    }

    public void EndBitEncoding()
    {
        _bitMode = false;
        ReloadBitBuffer();
    }

    private void ReloadBitBuffer()
    {
        if (_bitBufferIndex > 0)
        {
            _binaryWriter.Write(_bitBuffer);
        }
        _bitBuffer = 0;
        _bitBufferIndex = 0;
    }

    public void Dispose()
    {
        _binaryWriter.Dispose();
    }
}
