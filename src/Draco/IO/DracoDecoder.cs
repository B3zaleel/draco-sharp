using Draco.IO.Mesh;
using Draco.IO.Metadata;

namespace Draco.IO;

public class DracoDecoder
{
    public Draco Decode(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        return Decode(new BinaryReader(fs));
    }

    public Draco Decode(Stream stream)
    {
        return Decode(new BinaryReader(stream));
    }

    public Draco Decode(BinaryReader binaryReader)
    {
        using var decoderBuffer = new DecoderBuffer(binaryReader);
        var header = DecodeHeader(decoderBuffer);
        decoderBuffer.BitStreamVersion = header.Version;
        DracoMetadata? metadata = null;

        if (header.Version >= Constants.BitStreamVersion(1, 3) && (header.Flags & Constants.Metadata.FlagMask) == Constants.Metadata.FlagMask)
        {
            metadata = MetadataDecoder.Decode(decoderBuffer);
        }
        var connectivityDecoder = GetConnectivityDecoder(decoderBuffer, header);
        connectivityDecoder.BitStreamVersion = header.Version;
        connectivityDecoder.DecodeConnectivity(decoderBuffer);
        connectivityDecoder.DecodeAttributes(decoderBuffer);

        return new Draco
        {
            Header = header,
            Metadata = metadata,
            Attributes = connectivityDecoder.PointCloud!.Attributes
        };
    }

    private static DracoHeader DecodeHeader(DecoderBuffer decoderBuffer)
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

    private static ConnectivityDecoder GetConnectivityDecoder(DecoderBuffer decoderBuffer, DracoHeader header)
    {
        if (header!.EncoderType == Constants.EncodingType.PointCloud)
        {
            throw new NotImplementedException("Point cloud decoding is not implemented.");
        }
        else if (header.EncoderType == Constants.EncodingType.TriangularMesh)
        {
            if (header.EncoderMethod == Constants.EncodingMethod.SequentialEncoding)
            {
                return new MeshSequentialDecoder();
            }
            else if (header.EncoderMethod == Constants.EncodingMethod.EdgeBreakerEncoding)
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
                throw new InvalidDataException($"Unsupported encoder method {header.EncoderMethod}.");
            }
        }
        else
        {
            throw new InvalidDataException($"Unsupported encoder type {header.EncoderType}.");
        }
    }
}
