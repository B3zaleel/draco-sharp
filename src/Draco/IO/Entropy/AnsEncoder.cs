using Draco.IO.Extensions;

namespace Draco.IO.Entropy;

internal class AnsEncoder
{
    public byte[] Buffer { get; protected set; } = [];
    public int BufferOffset { get; protected set; } = 0;
    public uint State { get; protected set; } = 0;
    protected int LRAnsBase { get; set; } = Constants.LRAnsBase;

    public virtual void WriteInit(byte[] buffer)
    {
        Buffer = buffer;
        BufferOffset = 0;
        State = Constants.LRAnsBase;
    }

    public void RAbsWrite(bool value, byte p0)
    {
        byte p = (byte)(Constants.DracoAnsP8Precision - p0);
        var lS = value ? p : p0;

        if (State >= Constants.DracoAnsLBase / Constants.DracoAnsP8Precision * Constants.AnsIOBase * lS)
        {
            Buffer[BufferOffset++] = (byte)(State % Constants.AnsIOBase);
            State /= Constants.AnsIOBase;
        }
        uint quot = State / lS;
        uint rem = State % lS;
        State = quot * Constants.DracoAnsP8Precision + rem + (value ? 0U : p);
    }

    public int WriteEnd()
    {
        Assertions.ThrowIfNot(State >= LRAnsBase && State < LRAnsBase * Constants.AnsIOBase, "State is out of range");
        uint state = (uint)(State - LRAnsBase);

        if (state < 64)
        {
            Buffer[BufferOffset] = (byte)state;
            return BufferOffset + 1;
        }
        else if (state < 16384)
        {
            Ans.MemPutLE16(Buffer, BufferOffset, 16384 + state);
            return BufferOffset + 2;
        }
        else if (state < 4194304)
        {
            Ans.MemPutLE24(Buffer, BufferOffset, 8388608 + state);
            return BufferOffset + 3;
        }
        else if (state < 1073741824)
        {
            Ans.MemPutLE32(Buffer, BufferOffset, 3221225472 + state);
            return BufferOffset + 4;
        }
        else
        {
            Assertions.Throw("State is too large to be serialized");
            return BufferOffset;
        }
    }
}
