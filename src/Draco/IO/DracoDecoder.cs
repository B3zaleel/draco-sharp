using Draco.IO.Mesh;
using Draco.IO.Metadata;

namespace Draco.IO;

public class DracoDecoder
{
    public DracoHeader? Header { get; private set; }
    public MetadataElement[]? AttMetadata;
    public MetadataElement? FileMetadata;

    public void Decode(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        Decode(new BinaryReader(fs));
    }

    public void Decode(Stream stream)
    {
        Decode(new BinaryReader(stream));
    }

    public void Decode(BinaryReader binaryReader)
    {
        using var decoderBuffer = new DecoderBuffer(binaryReader);
        Header = ParseHeader(decoderBuffer);
        decoderBuffer.BitStream_Version = Header.Version;
        if (Header.Version >= Constants.BitStreamVersion(1, 3) && (Header.Flags & Constants.Metadata.FlagMask) == Constants.Metadata.FlagMask)
        {
            var metadataDecoder = new MetadataDecoder();
            metadataDecoder.Decode(decoderBuffer);
            AttMetadata = metadataDecoder.AttMetadata;
            FileMetadata = metadataDecoder.FileMetadata;
        }
        var connectivityDecoder = GetDecoder(buffer);
        connectivityDecoder.DecodeConnectivity(buffer);
    }

    private static DracoHeader ParseHeader(DecoderBuffer decoderBuffer)
    {
        var dracoMagic = decoderBuffer.ReadASCIIBytes(Constants.DracoMagic.Length);
        if (dracoMagic != Constants.DracoMagic)
        {
            throw new InvalidDataException("Invalid Draco file.");
        }
        var majorVersion = decoderBuffer.ReadByte();
        var minorVersion = decoderBuffer.ReadByte();
        var encoderType = decoderBuffer.ReadByte();
        var encoderMethod = decoderBuffer.ReadByte();
        var flags = decoderBuffer.ReadUInt16();

        return new DracoHeader(
            majorVersion: majorVersion,
            minorVersion: minorVersion,
            encoderType: encoderType,
            encoderMethod: encoderMethod,
            flags: flags
        );
    }

    private IConnectivityDecoder GetDecoder(DecoderBuffer decoderBuffer)
    {
        if (Header!.EncoderType == Constants.EncodingType.PointCloud)
        {
            throw new NotImplementedException("Point cloud decoding is not implemented.");
        }
        else if (Header.EncoderType == Constants.EncodingType.TriangularMesh)
        {
            if (Header.EncoderMethod == Constants.EncodingMethod.SequentialEncoding)
            {
                return new MeshSequentialDecoder();
            }
            else if (Header.EncoderMethod == Constants.EncodingMethod.EdgeBreakerEncoding)
            {
                var traversalDecoderType = decoderBuffer.ReadByte();

                return traversalDecoderType switch
                {
                    Constants.EdgeBreakerTraversalDecoderType.StandardEdgeBreaker => new MeshEdgeBreakerTraversalDecoder(),
                    Constants.EdgeBreakerTraversalDecoderType.ValenceEdgeBreaker => new MeshEdgeBreakerTraversalValenceDecoder(),
                    Constants.EdgeBreakerTraversalDecoderType.PredictiveEdgeBreaker => new MeshEdgeBreakerTraversalPredictiveDecoder(),
                    _ => throw new InvalidDataException($"Unsupported edge breaker traversal decoder type {traversalDecoderType}"),
                };
            }
            else
            {
                throw new InvalidDataException($"Unsupported encoder method {Header.EncoderMethod}.");
            }
        }
        else
        {
            throw new InvalidDataException($"Unsupported encoder type {Header.EncoderType}.");
        }
    }
}
