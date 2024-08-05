using Draco.IO.Extensions;

namespace Draco.IO.Core;

public class DataBuffer
{
    internal byte[] _data = [];
    public long Id { get; set; }
    public long UpdateCount { get; set; }
    public long DataSize => _data.Length;

    public void Update<T>(T[] data)
    {
        _data = new byte[data.Length * Constants.SizeOf<T>()];
        Write(data, 0);
    }

    public void Resize(long size)
    {
        Array.Resize(ref _data, (int)size);
    }

    public T Read<T>(long position)
    {
        var datumSize = Constants.SizeOf<T>();
        var data = _data.GetSubArray((int)position, datumSize);

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

    public T[] Read<T>(long position, int count)
    {
        var datumSize = Constants.SizeOf<T>();
        var data = _data.GetSubArray((int)position, count * datumSize);

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

    public void Write<T>(T datum, long position)
    {
        var convertedData = datum switch
        {
            bool datumAsBool => [(byte)(datumAsBool ? 1 : 0)],
            byte datumAsByte => [datumAsByte],
            sbyte datumAsSByte => [(byte)datumAsSByte],
            ushort datumAsUShort => BitConverter.GetBytes(datumAsUShort),
            short datumAsShort => BitConverter.GetBytes(datumAsShort),
            uint datumAsUInt => BitConverter.GetBytes(datumAsUInt),
            int datumAsInt => BitConverter.GetBytes(datumAsInt),
            ulong datumAsLong => BitConverter.GetBytes(datumAsLong),
            long datumAsLong => BitConverter.GetBytes(datumAsLong),
            float datumAsFloat => BitConverter.GetBytes(datumAsFloat),
            double datumAsDouble => BitConverter.GetBytes(datumAsDouble),
            _ => throw new NotImplementedException("")
        };
        var convertedDataBytes = convertedData!.ToArray();
        var size = position + convertedDataBytes.Length;
        if (size > _data.Length)
        {
            Resize(size);
        }
        _data.SetSubArray(convertedDataBytes, (int)position);
    }

    public void Write<T>(T[] data, long position)
    {
        var convertedData = data switch
        {
            bool[] datumAsBooleans => datumAsBooleans.Select(datum => (byte)(datum ? 1 : 0)),
            byte[] dataAsBytes => dataAsBytes,
            sbyte[] dataAsSBytes => dataAsSBytes.Select(datum => (byte)datum),
            ushort[] dataAsUShorts => dataAsUShorts.SelectMany(BitConverter.GetBytes),
            short[] dataAsShorts => dataAsShorts.SelectMany(BitConverter.GetBytes),
            uint[] dataAsUInts => dataAsUInts.SelectMany(BitConverter.GetBytes),
            int[] dataAsInts => dataAsInts.SelectMany(BitConverter.GetBytes),
            ulong[] dataAsULongs => dataAsULongs.SelectMany(BitConverter.GetBytes),
            long[] dataAsLongs => dataAsLongs.SelectMany(BitConverter.GetBytes),
            float[] dataAsFloats => dataAsFloats.SelectMany(BitConverter.GetBytes),
            double[] dataAsDoubles => dataAsDoubles.SelectMany(BitConverter.GetBytes),
            _ => throw new NotImplementedException("")
        };
        var convertedDataBytes = convertedData!.ToArray();
        var size = position + convertedDataBytes.Length;
        if (size > convertedDataBytes.Length)
        {
            Resize(size);
        }
        _data.SetSubArray(convertedDataBytes, (int)position);
    }

    public void Write(Stream stream)
    {
        stream.Write(_data);
    }
}
