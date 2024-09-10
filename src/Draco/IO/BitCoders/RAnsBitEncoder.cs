using Draco.IO.Entropy;
using Draco.IO.Extensions;

namespace Draco.IO.BitCoders;

internal class RAnsBitEncoder
{
    private List<ulong> _bitCounts = [0, 0];
    private readonly List<uint> _bits = [];
    private uint _localBits = 0;
    private byte _numLocalBits = 0;
    private EncoderBuffer? _encoderBuffer = null;

    public void StartEncoding()
    {
        Clear();
    }

    public void StartEncoding(EncoderBuffer encoderBuffer)
    {
        _encoderBuffer = encoderBuffer;
        Clear();
    }

    public void EncodeBit(bool bit)
    {
        if (bit)
        {
            _bitCounts[1]++;
            _localBits |= 1U << _numLocalBits;
        }
        else
        {
            _bitCounts[0]++;
        }
        _numLocalBits++;

        if (_numLocalBits == 32)
        {
            _bits.Add(_localBits);
            _localBits = 0;
            _numLocalBits = 0;
        }
    }

    public void EncodeLeastSignificantBits32(byte count, uint value)
    {
        Assertions.ThrowIfNot(count > 0, "Count must be greater than 0");
        Assertions.ThrowIfNot(count <= 32, "Count must be less than or equal to 32");
        uint reversedValue = BitUtilities.ReverseBits32(value) >> (32 - count);
        int ones = (int)BitUtilities.CountOneBits32(reversedValue);
        int remainingLocalBitsCount = 32 - _numLocalBits;
        _bitCounts[0] += (ulong)(count - ones);
        _bitCounts[1] += (ulong)ones;

        if (count <= remainingLocalBitsCount)
        {
            BitUtilities.CopyBits32(ref _localBits, _numLocalBits, reversedValue, 0, count);
            _numLocalBits += count;

            if (_numLocalBits == 32)
            {
                _bits.Add(_localBits);
                _localBits = 0;
                _numLocalBits = 0;
            }
        }
        else
        {
            BitUtilities.CopyBits32(ref _localBits, _numLocalBits, reversedValue, 0, remainingLocalBitsCount);
            _bits.Add(_localBits);
            _localBits = 0;
            BitUtilities.CopyBits32(ref _localBits, 0, reversedValue, remainingLocalBitsCount, count - remainingLocalBitsCount);
            _numLocalBits = (byte)(count - remainingLocalBitsCount);
        }
    }

    public void EndEncoding()
    {
        if (_encoderBuffer != null)
        {
            EndEncoding(_encoderBuffer);
        }
    }

    public void EndEncoding(EncoderBuffer encoderBuffer)
    {
        ulong totalBitsCount = _bitCounts[0] + _bitCounts[1];
        if (totalBitsCount == 0)
        {
            totalBitsCount++;
        }
        uint zeroProbRaw = Constants.StaticCast<double, uint>((_bitCounts[0] / Constants.StaticCast<ulong, double>(totalBitsCount) * 256.0) + 0.5);
        byte zeroProb = 255;
        if (zeroProbRaw < 255)
        {
            zeroProb = (byte)zeroProbRaw;
        }
        if (zeroProb == 0)
        {
            zeroProb++;
        }
        var buffer = new byte[(_bits.Count + 8) * 8];
        var ansEncoder = new AnsEncoder();
        ansEncoder.WriteInit(buffer);

        for (int i = _numLocalBits - 1; i >= 0; --i)
        {
            byte bit = (byte)((_localBits >> i) & 1);
            ansEncoder.RAbsWrite(bit != 0, zeroProb);
        }
        foreach (var bitCount in _bits)
        {
            for (int i = 31; i >= 0; --i)
            {
                byte bit = (byte)((bitCount >> i) & 1);
                ansEncoder.RAbsWrite(bit != 0, zeroProb);
            }
        }
        var sizeInBytes = ansEncoder.WriteEnd();
        encoderBuffer.WriteByte(zeroProb);
        encoderBuffer.EncodeVarIntUnsigned((uint)sizeInBytes);
        encoderBuffer.WriteBytes(buffer.GetSubArray(0, sizeInBytes));
        // encoderBuffer.EndBitEncoding();
        Clear();
    }

    private void Clear()
    {
        _bitCounts = [0, 0];
        _bits.Clear();
        _localBits = 0;
        _numLocalBits = 0;
    }
}
