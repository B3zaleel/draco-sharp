namespace Draco.IO;

internal class BitUtilities
{
    public static uint ReverseBits32(uint n)
    {
        n = ((n >> 1) & 0x55555555) | ((n & 0x55555555) << 1);
        n = ((n >> 2) & 0x33333333) | ((n & 0x33333333) << 2);
        n = ((n >> 4) & 0x0F0F0F0F) | ((n & 0x0F0F0F0F) << 4);
        n = ((n >> 8) & 0x00FF00FF) | ((n & 0x00FF00FF) << 8);
        return (n >> 16) | (n << 16);
    }

    public static ulong ConvertSignedIntToSymbol(long val)
    {
        if (val >= 0)
        {
            return (uint)(val << 1);
        }
        return ((ulong)(-val - 1) << 1) | 1;
    }

    public static uint ConvertSignedIntToSymbol(int val)
    {
        if (val >= 0)
        {
            return (uint)(val << 1);
        }
        return ((uint)(-val - 1) << 1) | 1;
    }

    public static long ConvertSymbolToSignedInt(ulong val)
    {
        var is_positive = (val & 1) != 0;
        val >>= 1;
        if (is_positive)
        {
            return (long)val;
        }
        return -(long)val - 1;
    }

    public static int ConvertSymbolToSignedInt(uint val)
    {
        var is_positive = (val & 1) != 0;
        val >>= 1;
        if (is_positive)
        {
            return (int)val;
        }
        return -(int)val - 1;
    }
}