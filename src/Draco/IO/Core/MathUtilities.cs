namespace Draco.IO.Core;

internal static class MathUtilities
{
    public static T AddAsUnsigned<T>(T a, T b)
    {
        var unsignedResult = typeof(T).Name switch
        {
            nameof(Byte) => (byte)(Convert.ToByte(a) + Convert.ToByte(b)),
            nameof(SByte) => (byte)(Convert.ToByte(a) + Convert.ToByte(b)),
            nameof(UInt16) => (ushort)(Convert.ToUInt16(a) + Convert.ToUInt16(b)),
            nameof(Int16) => (ushort)(Convert.ToUInt16(a) + Convert.ToUInt16(b)),
            nameof(UInt32) => Convert.ToUInt32(a) + Convert.ToUInt32(b),
            nameof(Int32) => Convert.ToUInt32(a) + Convert.ToUInt32(b),
            _ => throw new NotImplementedException("")
        };
        return (T)Convert.ChangeType(unsignedResult, typeof(T));
    }
}
