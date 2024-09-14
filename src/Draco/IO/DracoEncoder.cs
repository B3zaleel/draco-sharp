using Draco.IO.Attributes;
using Draco.IO.Enums;
using Draco.IO.Mesh;
using Draco.IO.Metadata;

namespace Draco.IO;

public class DracoEncoder
{
    public void Encode(string path, Config config, PointCloud.PointCloud connectedData, List<PointAttribute> attributes, DracoMetadata? metadata = null)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        Encode(new BinaryWriter(fs), config, connectedData, attributes, metadata);
    }

    public void Encode(Stream stream, Config config, PointCloud.PointCloud connectedData, List<PointAttribute> attributes, DracoMetadata? metadata = null)
    {
        Encode(new BinaryWriter(stream), config, connectedData, attributes, metadata);
    }

    public void Encode(BinaryWriter binaryWriter, Config config, PointCloud.PointCloud connectedData, List<PointAttribute> attributes, DracoMetadata? metadata = null)
    {
        var (encoderType, encoderMethod) = GetEncoderTypeAndMethod(config, connectedData);
        ushort flags = metadata == null ? (ushort)0 : Constants.Metadata.FlagMask;
        var header = new DracoHeader(Constants.MajorVersion, Constants.MinorVersion, encoderType, encoderMethod, flags);
        using var encoderBuffer = new EncoderBuffer(binaryWriter);
        encoderBuffer.BitStreamVersion = header.Version;
        EncodeHeader(encoderBuffer, header);

        if (metadata != null)
        {
            MetadataEncoder.Encode(encoderBuffer, metadata);
        }
        var connectivityEncoder = GetConnectivityEncoder(encoderBuffer, config, header, connectedData);
        connectivityEncoder.EncodeConnectivity(encoderBuffer);
        connectivityEncoder.EncodeAttributes(encoderBuffer);
        if (config.GetOption(ConfigOptionName.StoreNumberOfEncodedPoints, false))
        {
            connectivityEncoder.ComputeNumberOfEncodedPoints();
        }
    }

    private static (byte EncoderType, byte EncoderMethod) GetEncoderTypeAndMethod(Config config, PointCloud.PointCloud connectivityData)
    {
        var encoderType = connectivityData is Mesh.Mesh ? Constants.EncodingType.TriangularMesh : Constants.EncodingType.PointCloud;
        int encoderMethod = config.GetOption(ConfigOptionName.EncodingMethod, -1);

        if (encoderType == Constants.EncodingType.TriangularMesh)
        {
            if (encoderMethod == -1)
            {
                encoderMethod = config.Speed == 10 ? Constants.EncodingMethod.SequentialEncoding : Constants.EncodingMethod.EdgeBreakerEncoding;
            }
        }

        return (encoderType, (byte)encoderMethod);
    }

    private static void EncodeHeader(EncoderBuffer encoderBuffer, DracoHeader header)
    {
        encoderBuffer.WriteASCIIBytes(Constants.DracoMagic);
        encoderBuffer.WriteByte(header.MajorVersion);
        encoderBuffer.WriteByte(header.MinorVersion);
        encoderBuffer.WriteByte(header.EncoderType);
        encoderBuffer.WriteByte(header.EncoderMethod);
        encoderBuffer.WriteUInt16(header.Flags);
    }

    private static ConnectivityEncoder GetConnectivityEncoder(EncoderBuffer encoderBuffer, Config config, DracoHeader header, PointCloud.PointCloud connectedData)
    {
        if (header!.EncoderType == Constants.EncodingType.PointCloud)
        {
            throw new NotImplementedException("Point cloud decoding is not implemented.");
        }
        else if (header.EncoderType == Constants.EncodingType.TriangularMesh)
        {
            var mesh = (Mesh.Mesh)connectedData!;

            if (header.EncoderMethod == Constants.EncodingMethod.SequentialEncoding)
            {
                return new MeshSequentialEncoder(config, mesh);
            }
            else if (header.EncoderMethod == Constants.EncodingMethod.EdgeBreakerEncoding)
            {
                var isTinyMesh = mesh.FacesCount < 1000;
                int selectedEdgeBreakerMethod = config.GetOption(ConfigOptionName.EdgeBreakerMethod, -1);

                if (selectedEdgeBreakerMethod == -1)
                {
                    if (config.Speed >= 5 || isTinyMesh)
                    {
                        selectedEdgeBreakerMethod = Constants.EdgeBreakerTraversalDecoderType.StandardEdgeBreaker;
                    }
                    else
                    {
                        selectedEdgeBreakerMethod = Constants.EdgeBreakerTraversalDecoderType.ValenceEdgeBreaker;
                    }
                }

                var meshEdgeBreakerEncoder = selectedEdgeBreakerMethod switch
                {
                    Constants.EdgeBreakerTraversalDecoderType.StandardEdgeBreaker => new MeshEdgeBreakerTraversalEncoder(config, mesh),
                    Constants.EdgeBreakerTraversalDecoderType.ValenceEdgeBreaker => new MeshEdgeBreakerTraversalValenceEncoder(config, mesh),
                    Constants.EdgeBreakerTraversalDecoderType.PredictiveEdgeBreaker => new MeshEdgeBreakerTraversalPredictiveEncoder(config, mesh),
                    _ => throw new InvalidDataException($"Unsupported edge breaker traversal encoder type {selectedEdgeBreakerMethod}"),
                };
                encoderBuffer.WriteByte((byte)selectedEdgeBreakerMethod);
                return meshEdgeBreakerEncoder;
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
