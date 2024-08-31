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

    public static void MemPutLE16(byte[] data, int offset, uint value)
    {
        data[offset + 0] = (byte)((value >> 0) & 0xFF);
        data[offset + 1] = (byte)((value >> 8) & 0xFF);
    }

    public static void MemPutLE24(byte[] data, int offset, uint value)
    {
        data[offset + 0] = (byte)((value >> 0) & 0xFF);
        data[offset + 1] = (byte)((value >> 8) & 0xFF);
        data[offset + 2] = (byte)((value >> 16) & 0xFF);
    }

    public static void MemPutLE32(byte[] data, int offset, uint value)
    {
        data[offset + 0] = (byte)((value >> 0) & 0xFF);
        data[offset + 1] = (byte)((value >> 8) & 0xFF);
        data[offset + 2] = (byte)((value >> 16) & 0xFF);
        data[offset + 3] = (byte)((value >> 24) & 0xFF);
    }
}
