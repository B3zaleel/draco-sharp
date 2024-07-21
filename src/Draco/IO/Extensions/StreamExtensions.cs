namespace Draco.IO.Extensions;

internal static class StreamExtensions
{
    public static Stream Create<T>(T[] data)
    {
        var convertedData = typeof(T).Name switch
        {
            nameof(Byte) => data as byte[],
            nameof(SByte) => (data as sbyte[])!.Select(datum => (byte)datum),
            nameof(UInt16) => (data as ushort[])!.SelectMany(BitConverter.GetBytes),
            nameof(Int16) => (data as short[])!.SelectMany(BitConverter.GetBytes),
            nameof(UInt32) => (data as uint[])!.SelectMany(BitConverter.GetBytes),
            nameof(Int32) => (data as int[])!.SelectMany(BitConverter.GetBytes),
            _ => throw new NotImplementedException("")
        };
        var buffer = convertedData!.ToArray();
        var stream = new MemoryStream(buffer)
        {
            Position = 0
        };
        return stream;
    }

    public static T ReadDatum<T>(this Stream stream)
    {
        var datumSize = typeof(T).Name switch
        {
            nameof(Byte) => sizeof(byte),
            nameof(SByte) => sizeof(sbyte),
            nameof(UInt16) => sizeof(ushort),
            nameof(Int16) => sizeof(short),
            nameof(UInt32) => sizeof(uint),
            nameof(Int32) => sizeof(int),
            _ => throw new NotImplementedException("")
        };
        var data = new byte[datumSize];
        stream.Read(data, 0, datumSize);
        return typeof(T).Name switch
        {
            nameof(Byte) => (T)Convert.ChangeType(data[0], typeof(T)),
            nameof(SByte) => (T)Convert.ChangeType((sbyte)data[0], typeof(T)),
            nameof(UInt16) => (T)Convert.ChangeType(BitConverter.ToUInt16(data), typeof(T)),
            nameof(Int16) => (T)Convert.ChangeType(BitConverter.ToInt16(data), typeof(T)),
            nameof(UInt32) => (T)Convert.ChangeType(BitConverter.ToUInt32(data), typeof(T)),
            nameof(Int32) => (T)Convert.ChangeType(BitConverter.ToInt32(data), typeof(T)),
            _ => throw new NotImplementedException()
        };
    }

    public static T[] ReadData<T>(this Stream stream, int count)
    {
        var datumSize = typeof(T).Name switch
        {
            nameof(Byte) => sizeof(byte),
            nameof(SByte) => sizeof(sbyte),
            nameof(UInt16) => sizeof(ushort),
            nameof(Int16) => sizeof(short),
            nameof(UInt32) => sizeof(uint),
            nameof(Int32) => sizeof(int),
            _ => throw new NotImplementedException("")
        };
        var size = datumSize * count;
        var data = new byte[size];
        stream.Read(data, 0, size);
        return typeof(T).Name switch
        {
            nameof(Byte) => (T[])Convert.ChangeType(data, typeof(T[])),
            nameof(SByte) => (T[])Convert.ChangeType(data.Select(datum => (sbyte)datum).ToArray(), typeof(T[])),
            nameof(UInt16) => (T[])Convert.ChangeType(data.Chunk(datumSize).Select(datumBytes => BitConverter.ToUInt16(datumBytes)).ToArray(), typeof(T[])),
            nameof(Int16) => (T[])Convert.ChangeType(data.Chunk(datumSize).Select(datumBytes => BitConverter.ToInt16(datumBytes)).ToArray(), typeof(T[])),
            nameof(UInt32) => (T[])Convert.ChangeType(data.Chunk(datumSize).Select(datumBytes => BitConverter.ToUInt32(datumBytes)).ToArray(), typeof(T[])),
            nameof(Int32) => (T[])Convert.ChangeType(data.Chunk(datumSize).Select(datumBytes => BitConverter.ToInt32(datumBytes)).ToArray(), typeof(T[])),
            _ => throw new NotImplementedException()
        };
    }

    public static void WriteData<T>(this Stream stream, T[] data)
    {
        var convertedData = typeof(T).Name switch
        {
            nameof(Byte) => data as byte[],
            nameof(SByte) => (data as sbyte[])!.Select(datum => (byte)datum),
            nameof(UInt16) => (data as ushort[])!.SelectMany(BitConverter.GetBytes),
            nameof(Int16) => (data as short[])!.SelectMany(BitConverter.GetBytes),
            nameof(UInt32) => (data as uint[])!.SelectMany(BitConverter.GetBytes),
            nameof(Int32) => (data as int[])!.SelectMany(BitConverter.GetBytes),
            _ => throw new NotImplementedException("")
        };
        stream.Write(convertedData!.ToArray());
    }
}
