namespace Draco.IO;

internal static class Constants
{
    public const string DracoMagic = "DRACO";
    public const byte MajorVersion = 2;
    public const byte MinorVersion = 2;

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
        public const sbyte LeftFaceEdge = 0;
        public const byte RightFaceEdge = 1;
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

    public enum NormalPredictionMode : byte
    {
        OneTriangle = 0,
        TriangleArea = 1,
    }

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
}

public enum GeometryAttributeType : sbyte
{
    Invalid = -1,
    Position = 0,
    Normal = 1,
    Color = 2,
    TexCoord = 3,
    Generic,
    NamedAttributesCount
}

/// <summary>
/// Represents different variants of <see cref="Mesh.Mesh"/> attributes.
/// </summary>
public enum MeshAttributeElementType : byte
{
    /// <summary>
    /// All corners attached to a vertex share the same attribute value. A typical example are the vertex positions and often vertex colors.
    /// </summary>
    VertexAttribute = 0,
    /// <summary>
    /// The most general attribute where every corner of the mesh can have a different attribute value. Often used for texture coordinates or normals.
    /// </summary>
    CornerAttribute = 1,
    /// <summary>
    /// All corners of a single face share the same value.
    /// </summary>
    FaceAttribute = 2
}

public enum AttributeTransformType : sbyte
{
    InvalidTransform = -1,
    NoTransform = 0,
    QuantizationTransform = 1,
    OctahedronTransform = 2,
}

public enum SequentialAttributeEncoderType : byte
{
    Generic = 0,
    Integer,
    Quantization,
    Normals
}

public enum PredictionSchemeMethod : sbyte
{
    None = -2,
    Undefined = -1,
    Difference = 0,
    Parallelogram = 1,
    MultiParallelogram = 2,
    TexCoordsDeprecated = 3,
    ConstrainedMultiParallelogram = 4,
    TexCoordsPortable = 5,
    GeometricNormal = 6,
    Count
}

public enum PredictionSchemeTransformType : sbyte
{
    None = -1,
    Delta = 0,
    Wrap = 1,
    NormalOctahedron = 2,
    NormalOctahedronCanonicalized = 3,
    Count,
}

public enum MeshTraversalMethod : byte
{
    DepthFirst = 0,
    PredictionDegree,
    Count
}

public enum DataType : byte
{
    Invalid = 0,
    Int8,
    UInt8,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Int64,
    UInt64,
    Float32,
    Float64,
    Bool,
    Count
}
