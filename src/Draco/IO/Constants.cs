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

    public static class SymbolCoding
    {
        public const byte Tagged = 0;
        public const byte Raw = 1;
    }
}

public enum GeometryAttributeType : sbyte
{
    Invalid = -1,
    Position = 0,
    Normal = 1,
    Color = 2,
    TexCoord = 3,
    NamedAttributesCount = 4
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
