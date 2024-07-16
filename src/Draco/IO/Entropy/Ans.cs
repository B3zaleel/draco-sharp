namespace Draco.IO.Entropy;

internal static class Ans
{
    public static uint MemGetLE16(byte[] data)
    {
        uint value = (uint)data[1] << 8;
        value |= data[0];
        return value;
    }

    public static uint MemGetLE24(byte[] data)
    {
        var value = (uint)data[2] << 16;
        value |= (uint)data[1] << 8;
        value |= data[0];
        return value;
    }

    public static uint MemGetLE32(byte[] data)
    {
        var value = (uint)data[3] << 24;
        value |= (uint)data[2] << 16;
        value |= (uint)data[1] << 8;
        value |= data[0];
        return value;
    }
}
