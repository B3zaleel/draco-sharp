namespace Draco.IO;

internal class BitUtilities
{
    /// <summary>
    /// Counts the number of '1' bits in a 32-bit unsigned integer.
    /// </summary>
    /// <param name="n">The integer.</param>
    /// <returns></returns>
    public static uint CountOneBits32(uint n)
    {
        n -= (n >> 1) & 0x55555555;
        n = ((n >> 2) & 0x33333333) + (n & 0x33333333);
        return (((n + (n >> 4)) & 0xF0F0F0F) * 0x1010101) >> 24;
    }

    public static uint ReverseBits32(uint n)
    {
        n = ((n >> 1) & 0x55555555) | ((n & 0x55555555) << 1);
        n = ((n >> 2) & 0x33333333) | ((n & 0x33333333) << 2);
        n = ((n >> 4) & 0x0F0F0F0F) | ((n & 0x0F0F0F0F) << 4);
        n = ((n >> 8) & 0x00FF00FF) | ((n & 0x00FF00FF) << 8);
        return (n >> 16) | (n << 16);
    }

    public static int MostSignificantBit(uint num)
    {
        int msb = -1;
        while (num != 0)
        {
            msb++;
            num >>= 1;
        }
        return msb;
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
        var isPositive = (val & 1) == 0;
        val >>= 1;
        if (isPositive)
        {
            return (long)val;
        }
        return -(long)val - 1;
    }

    public static int ConvertSymbolToSignedInt(uint val)
    {
        var isPositive = (val & 1) == 0;
        val >>= 1;
        if (isPositive)
        {
            return (int)val;
        }
        return -(int)val - 1;
    }

    public static uint[] ConvertSignedIntsToSymbols(int[] signedInts)
    {
        var symbols = new uint[signedInts.Length];

        for (int i = 0; i < signedInts.Length; ++i)
        {
            symbols[i] = ConvertSignedIntToSymbol(signedInts[i]);
        }
        return symbols;
    }

    public static int[] ConvertSymbolsToSignedInts(uint[] symbols)
    {
        var signedInts = new int[symbols.Length];

        for (int i = 0; i < symbols.Length; ++i)
        {
            signedInts[i] = ConvertSymbolToSignedInt(symbols[i]);
        }
        return signedInts;
    }
}
