using Draco.IO.Enums;

namespace Draco.IO;

internal static class Constants
{
    public const string DracoMagic = "DRACO";
    public const byte MajorVersion = 2;
    public const byte MinorVersion = 2;
    public const string BitOperationDisallowedMessage = "Cannot execute this whilst bit mode is not on";
    public const string NonBitOperationDisallowedMessage = "Cannot execute this whilst bit mode is on";
    /// <summary>
    /// The default maximum speed for both encoding/decoding.
    /// </summary>
    public const byte DefaultSpeed = 5;

    public static ushort BitStreamVersion(byte majorVersion, byte minorVersion) => (ushort)((majorVersion << 8) | minorVersion);

    public static class Metadata
    {
        public const ushort FlagMask = 32768;
    }

    public static class EncodingType
    {
        public const byte PointCloud = 0;
        public const byte TriangularMesh = 1;
    }

    public static class EncodingMethod
    {
        public const byte SequentialEncoding = 0;
        public const byte EdgeBreakerEncoding = 1;
    }

    public static class SequentialIndicesEncodingMethod
    {
        public const byte CompressedIndices = 0;
        public const byte UncompressedIndices = 1;
    }

    public static class EdgeBreakerTraversalDecoderType
    {
        public const byte StandardEdgeBreaker = 0;
        public const byte PredictiveEdgeBreaker = 1;
        public const byte ValenceEdgeBreaker = 2;
    }

    // EdgeBreaker constants
    // public const sbyte kInvalidCornerIndex = -1;
    public const uint kInvalidCornerIndex = uint.MaxValue;
    public const uint kInvalidVertexIndex = uint.MaxValue;
    public const uint kInvalidFaceIndex = uint.MaxValue;
    public const uint kInvalidAttributeValueIndex = uint.MaxValue;

    public static class EdgeFaceName
    {
        public const byte LeftFaceEdge = 0;
        public const byte RightFaceEdge = 1;
    }

    public static readonly byte[] EdgeBreakerTopologyBitPatternLength = [1, 3, 0, 3, 0, 3, 0, 3];

    public static readonly byte[] EdgeBreakerTopologyToSymbolId =
    [
        EdgeBreakerSymbol.C,
        EdgeBreakerSymbol.S,
        EdgeBreakerSymbol.Invalid,
        EdgeBreakerSymbol.L,
        EdgeBreakerSymbol.Invalid,
        EdgeBreakerSymbol.R,
        EdgeBreakerSymbol.Invalid,
        EdgeBreakerSymbol.E
    ];

    public static class EdgeBreakerSymbol
    {
        public const byte C = 0;
        public const byte S = 1;
        public const byte L = 2;
        public const byte R = 3;
        public const byte E = 4;
        public const byte Invalid = 5;
    }

    public static readonly byte[] EdgeBreakerSymbolToTopologyId =
    [
        EdgeBreakerTopologyBitPattern.C,
        EdgeBreakerTopologyBitPattern.S,
        EdgeBreakerTopologyBitPattern.L,
        EdgeBreakerTopologyBitPattern.R,
        EdgeBreakerTopologyBitPattern.E
    ];

    public static class EdgeBreakerTopologyBitPattern
    {
        public const byte C = 0;
        public const byte S = 1;
        public const byte L = 3;
        public const byte R = 5;
        public const byte E = 7;
        public const byte InitFace = 8;
        public const byte Invalid = 9;
    }

    public static class EdgeBreakerValenceCodingMode
    {
        public const sbyte EdgeBreakerValenceMode_2_7 = 0;
    }

    // Valence EdgeBreaker constants
    public const byte MinValence = 2;
    public const byte MaxValence = 7;
    public const byte NumUniqueValences = 6;
    // ANS constants
    public const ushort DracoAnsP8Precision = 256;
    public const ushort DracoAnsP10Precision = 1024;
    public const ushort DracoAnsLBase = 4096;
    public const ushort AnsIOBase = 256;
    public const ushort LRAnsBase = 4096;
    public const ushort TaggedRAnsBase = 16384;
    public const ushort TaggedRAnsPrecision = 4096;
    public const byte MaxTagSymbolBitLength = 32;
    public const byte MaxRawEncodingBitLength = 18;
    public const byte DefaultSymbolCodingCompressionLevel = 7;
    // Encoding constants
    public const byte ConstrainedMultiParallelogramMaxNumParallelograms = 4;

    public static class SymbolCoding
    {
        public const byte Tagged = 0;
        public const byte Raw = 1;
    }

    public static int DataTypeLength(DataType dataType)
    {
        return dataType switch
        {
            DataType.Int8 or DataType.UInt8 or DataType.Bool => 1,
            DataType.Int16 or DataType.UInt16 => 2,
            DataType.Int32 or DataType.UInt32 or DataType.Float32 => 4,
            DataType.Int64 or DataType.UInt64 or DataType.Float64 => 8,
            _ => -1
        };
    }

    public static bool IsDataTypeIntegral(DataType dataType)
    {
        return dataType switch
        {
            DataType.Int8 or DataType.UInt8 or DataType.Int16 or DataType.UInt16 or DataType.Int32 or DataType.UInt32 or DataType.Int64 or DataType.UInt64 or DataType.Bool => true,
            _ => false
        };
    }

    public static bool IsIntegral<T>()
    {
        return typeof(T) == typeof(sbyte) || typeof(T) == typeof(byte) || typeof(T) == typeof(short) || typeof(T) == typeof(ushort) || typeof(T) == typeof(int) || typeof(T) == typeof(uint) || typeof(T) == typeof(long) || typeof(T) == typeof(ulong) || typeof(T) == typeof(bool) || typeof(T) == typeof(char);
    }

    public static bool IsFloatingPoint<T>()
    {
        return typeof(T) == typeof(float) || typeof(T) == typeof(double) || typeof(T) == typeof(Half);
    }

    public static byte SizeOf<T>()
    {
        return typeof(T) switch
        {
            Type t when t == typeof(sbyte) || t == typeof(byte) => 1,
            Type t when t == typeof(short) || t == typeof(ushort) || t == typeof(char) || t == typeof(Half) => 2,
            Type t when t == typeof(int) || t == typeof(uint) || t == typeof(float) => 4,
            Type t when t == typeof(long) || t == typeof(ulong) || t == typeof(double) => 8,
            _ => throw new NotImplementedException()
        };
    }

    public static TOut ConstCast<TIn, TOut>(TIn value)
    {
        return (TOut)Convert.ChangeType(value, typeof(TOut))!;
    }

    public static TOut StaticCast<TIn, TOut>(TIn value)
    {
        var valueBytes = value switch
        {
            sbyte v => [v < 0 ? (byte)(v + 256) : (byte)v],
            byte v => [v],
            short v => BitConverter.GetBytes(v),
            ushort v => BitConverter.GetBytes(v),
            int v => BitConverter.GetBytes(v),
            uint v => BitConverter.GetBytes(v),
            long v => BitConverter.GetBytes(v),
            ulong v => BitConverter.GetBytes(v),
            float v => BitConverter.GetBytes(v),
            double v => BitConverter.GetBytes(v),
            _ => throw new NotImplementedException()
        };
        var interpretedValue = typeof(TOut).Name switch
        {
            nameof(SByte) => (TOut)(object)(sbyte)(valueBytes[0] > 127 ? valueBytes[0] - 256 : valueBytes[0]),
            nameof(Byte) => (TOut)(object)valueBytes[0],
            nameof(Int16) => (TOut)(object)BitConverter.ToInt16(valueBytes, 0),
            nameof(UInt16) => (TOut)(object)BitConverter.ToUInt16(valueBytes, 0),
            nameof(Int32) => (TOut)(object)BitConverter.ToInt32(valueBytes, 0),
            nameof(UInt32) => (TOut)(object)BitConverter.ToUInt32(valueBytes, 0),
            nameof(Int64) => (TOut)(object)BitConverter.ToInt64(valueBytes, 0),
            nameof(UInt64) => (TOut)(object)BitConverter.ToUInt64(valueBytes, 0),
            nameof(Single) => (TOut)(object)BitConverter.ToSingle(valueBytes, 0),
            nameof(Double) => (TOut)(object)BitConverter.ToDouble(valueBytes, 0),
            _ => throw new NotImplementedException()
        };
        return interpretedValue;
    }

    public static TOut[] ReinterpretCast<TIn, TOut>(TIn[] values)
    {
        var result = new TOut[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            result[i] = StaticCast<TIn, TOut>(values[i]);
        }
        return result;
    }
}
