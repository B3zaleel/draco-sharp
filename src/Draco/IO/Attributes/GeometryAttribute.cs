using System.Numerics;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes;

public class GeometryAttribute
{
    public Stream? Buffer { get; set; }
    public GeometryAttributeType AttributeType { get; set; }
    public byte NumComponents { get; set; }
    public DataType DataType { get; set; }
    public bool Normalized { get; set; }
    public long ByteStride { get; set; }
    public long ByteOffset { get; set; }
    public uint UniqueId { get; set; }

    internal GeometryAttribute() : this(GeometryAttributeType.Invalid, null, 1, DataType.Float32, false, 0, 0) { }

    internal GeometryAttribute(GeometryAttributeType attributeType, Stream? buffer, byte numComponents, DataType dataType, bool normalized, long byteStride, long byteOffset)
    {
        Buffer = buffer;
        AttributeType = attributeType;
        NumComponents = numComponents;
        DataType = dataType;
        Normalized = normalized;
        ByteStride = byteStride;
        ByteOffset = byteOffset;
    }

    public void CopyFrom(GeometryAttribute srcAttribute)
    {
        AttributeType = srcAttribute.AttributeType;
        NumComponents = srcAttribute.NumComponents;
        DataType = srcAttribute.DataType;
        Normalized = srcAttribute.Normalized;
        ByteStride = srcAttribute.ByteStride;
        ByteOffset = srcAttribute.ByteOffset;
        UniqueId = srcAttribute.UniqueId;

        if (srcAttribute.Buffer == null)
        {
            Buffer = null;
        }
        else
        {
            Buffer = new MemoryStream();
            Buffer.Update(srcAttribute.Buffer);
        }
    }

    public void ResetBuffer(Stream buffer, long byteStride, long byteOffset)
    {
        Buffer = buffer;
        ByteStride = byteStride;
        ByteOffset = byteOffset;
    }

    public T GetValue<T>()
    {
        return Buffer!.ReadDatum<T>();
    }

    public T[] GetValue<T>(uint attributeId, int numComponents)
    {
        Buffer!.Seek(ByteOffset + ByteStride * attributeId, SeekOrigin.Begin);
        return Buffer!.ReadData<T>(numComponents);
    }

    public TOut[] ConvertValue<TOut>(uint attributeId)
        where TOut : struct,
            IComparisonOperators<TOut, TOut, bool>,
            IComparable,
            IEqualityOperators<TOut, TOut, bool>,
            IAdditionOperators<TOut, TOut, TOut>,
            ISubtractionOperators<TOut, TOut, TOut>,
            IDivisionOperators<TOut, TOut, TOut>,
            IMultiplyOperators<TOut, TOut, TOut>,
            IDecrementOperators<TOut>,
            IBitwiseOperators<TOut, TOut, TOut>,
            IMinMaxValue<TOut>
    {
        return ConvertValue<TOut>(attributeId, NumComponents);
    }

    public TOut[] ConvertValue<TOut>(uint attributeId, int numComponents)
        where TOut : struct,
            IComparisonOperators<TOut, TOut, bool>,
            IComparable,
            IEqualityOperators<TOut, TOut, bool>,
            IAdditionOperators<TOut, TOut, TOut>,
            ISubtractionOperators<TOut, TOut, TOut>,
            IDivisionOperators<TOut, TOut, TOut>,
            IMultiplyOperators<TOut, TOut, TOut>,
            IDecrementOperators<TOut>,
            IBitwiseOperators<TOut, TOut, TOut>,
            IMinMaxValue<TOut>
    {
        return DataType switch
        {
            DataType.Int8 => ConvertTypedValue<sbyte, TOut>(attributeId, numComponents),
            DataType.UInt8 => ConvertTypedValue<byte, TOut>(attributeId, numComponents),
            DataType.Int16 => ConvertTypedValue<short, TOut>(attributeId, numComponents),
            DataType.UInt16 => ConvertTypedValue<ushort, TOut>(attributeId, numComponents),
            DataType.Int32 => ConvertTypedValue<int, TOut>(attributeId, numComponents),
            DataType.UInt32 => ConvertTypedValue<uint, TOut>(attributeId, numComponents),
            DataType.Int64 => ConvertTypedValue<long, TOut>(attributeId, numComponents),
            DataType.UInt64 => ConvertTypedValue<ulong, TOut>(attributeId, numComponents),
            DataType.Float32 => ConvertTypedValue<float, TOut>(attributeId, numComponents),
            DataType.Float64 => ConvertTypedValue<double, TOut>(attributeId, numComponents),
            // DataType.Bool => ConvertTypedValue<bool, TOut>(attributeId, numComponents),
            _ => throw new InvalidOperationException("Invalid data type.")
        };
    }

    public TOut[] ConvertTypedValue<T, TOut>(uint attributeId, int numComponents)
        where T : struct,
            IComparisonOperators<T, T, bool>,
            IComparable,
            IEqualityOperators<T, T, bool>,
            IAdditionOperators<T, T, T>,
            ISubtractionOperators<T, T, T>,
            IDivisionOperators<T, T, T>,
            IMultiplyOperators<T, T, T>,
            IDecrementOperators<T>,
            IBitwiseOperators<T, T, T>,
            IMinMaxValue<T>
        where TOut : struct,
            IComparisonOperators<TOut, TOut, bool>,
            IComparable,
            IEqualityOperators<TOut, TOut, bool>,
            IAdditionOperators<TOut, TOut, TOut>,
            ISubtractionOperators<TOut, TOut, TOut>,
            IDivisionOperators<TOut, TOut, TOut>,
            IMultiplyOperators<TOut, TOut, TOut>,
            IDecrementOperators<TOut>,
            IBitwiseOperators<TOut, TOut, TOut>,
            IMinMaxValue<TOut>
    {
        var size = Math.Min(numComponents, NumComponents);
        var result = new TOut[size];

        for (int i = 0; i < size; ++i)
        {
            var value = Buffer!.ReadDatum<T>();
            result[i] = ConvertComponentValue<T, TOut>(value, Normalized);
        }
        return result;
    }

    private static TOut ConvertComponentValue<T, TOut>(T value, bool normalized)
        where T : struct,
            IComparisonOperators<T, T, bool>,
            IComparable,
            IEqualityOperators<T, T, bool>,
            IAdditionOperators<T, T, T>,
            ISubtractionOperators<T, T, T>,
            IDivisionOperators<T, T, T>,
            IMultiplyOperators<T, T, T>,
            IDecrementOperators<T>,
            IBitwiseOperators<T, T, T>,
            IMinMaxValue<T>
        where TOut : struct,
            IComparisonOperators<TOut, TOut, bool>,
            IComparable,
            IEqualityOperators<TOut, TOut, bool>,
            IAdditionOperators<TOut, TOut, TOut>,
            ISubtractionOperators<TOut, TOut, TOut>,
            IDivisionOperators<TOut, TOut, TOut>,
            IMultiplyOperators<TOut, TOut, TOut>,
            IDecrementOperators<TOut>,
            IBitwiseOperators<TOut, TOut, TOut>,
            IMinMaxValue<TOut>
    {
        if (Constants.IsIntegral<T>() && Constants.IsFloatingPoint<TOut>() && normalized)
        {
            var result = (TOut)Convert.ChangeType(value, typeof(TOut))!;
            result /= (TOut)Convert.ChangeType(T.MaxValue, typeof(TOut))!;
            return result;
        }
        else if (Constants.IsFloatingPoint<T>() && Constants.IsIntegral<TOut>() && normalized)
        {
            Assertions.ThrowIf(value < (T)Convert.ChangeType(0, typeof(T))! || value > (T)Convert.ChangeType(1, typeof(T))!);
            Assertions.ThrowIf(Constants.SizeOf<T>() > 4);
            var result = (TOut)Convert.ChangeType(Math.Floor((double)Convert.ChangeType(value, typeof(double))! * (double)Convert.ChangeType(TOut.MaxValue, typeof(double))! + 0.5), typeof(TOut))!;
            return result;
        }
        else
        {
            var result = (TOut)Convert.ChangeType(value, typeof(TOut))!;
            return result;
        }
    }
}
