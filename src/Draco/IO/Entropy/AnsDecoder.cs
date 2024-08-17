using Draco.IO.Extensions;

namespace Draco.IO.Entropy;

internal class AnsDecoder
{
    public byte[] Buffer { get; protected set; } = [];
    public int BufferOffset { get; protected set; } = 0;
    public uint State { get; protected set; } = 0;

    public virtual void ReadInit(byte[] buffer, int offset)
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
        else
        {
            Assertions.Throw("Invalid data");
        }
        State += Constants.DracoAnsLBase;
        Assertions.ThrowIf(State >= Constants.DracoAnsLBase * Constants.AnsIOBase, "Invalid state");
    }

    public bool RAbsRead(byte p0)
    {
        byte p = (byte)(Constants.DracoAnsP8Precision - p0);
        if (State < Constants.DracoAnsLBase && BufferOffset > 0)
        {
            State = State * Constants.AnsIOBase + Buffer[--BufferOffset];
        }
        uint x = State;
        uint quot = x / Constants.DracoAnsP8Precision;
        uint rem = x % Constants.DracoAnsP8Precision;
        uint xn = quot * p;
        var val = rem < p;
        State = val ? xn + rem : x - xn - p;
        return val;
    }

    public void ReadEnd()
    {
        State = Constants.LRAnsBase;
    }
}
