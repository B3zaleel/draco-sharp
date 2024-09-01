using Draco.IO.Enums;
using Draco.IO.Mesh;
using Draco.IO.Metadata;

namespace Draco.IO;

public class DracoEncoder
{
    public void Encode(string path, Config config, Draco draco)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        Encode(new BinaryWriter(fs), config, draco);
    }

    public void Encode(Stream stream, Config config, Draco draco)
    {
        Encode(new BinaryWriter(stream), config, draco);
    }

    public void Encode(BinaryWriter binaryWriter, Config config, Draco draco)
    {
        using var encoderBuffer = new EncoderBuffer(binaryWriter);
        EncodeHeader(encoderBuffer, draco.Header);
        encoderBuffer.BitStreamVersion = draco.Header.Version;

        if (draco.Header.Version >= Constants.BitStreamVersion(1, 3) && (draco.Header.Flags & Constants.Metadata.FlagMask) == Constants.Metadata.FlagMask)
        {
            MetadataEncoder.Encode(encoderBuffer, draco.Metadata!);
        }
        var connectivityEncoder = GetConnectivityEncoder(encoderBuffer, config, draco.ConnectedData);
        connectivityEncoder.EncodeConnectivity(encoderBuffer);
        connectivityEncoder.EncodeAttributes(encoderBuffer);
        if (config.GetOption(ConfigOptionName.StoreNumberOfEncodedPoints, false))
        {
            connectivityEncoder.ComputeNumberOfEncodedPoints();
        }
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

    private static ConnectivityEncoder GetConnectivityEncoder(EncoderBuffer encoderBuffer, Config config, PointCloud.PointCloud pointCloud)
    {
        var isStandardEdgeBreakerAvailable = config.IsOptionSet(ConfigOptionName.Feature.EdgeBreaker);
        var isPredictiveEdgeBreakerAvailable = config.IsOptionSet(ConfigOptionName.Feature.PredictiveEdgeBreaker);
        var encodingMethod = config.GetOption(ConfigOptionName.EncodingMethod, -1);

        if (pointCloud is Mesh.Mesh mesh)
        {
            if (encodingMethod == -1)
            {
                encodingMethod = config.Speed == 10 ? Constants.EncodingMethod.SequentialEncoding : Constants.EncodingMethod.EdgeBreakerEncoding;
            }

            if (encodingMethod == Constants.EncodingMethod.EdgeBreakerEncoding)
            {
                var isTinyMesh = mesh.FacesCount < 1000;
                int selectedEdgeBreakerMethod = config.GetOption(ConfigOptionName.EdgeBreakerMethod, -1);

                if (selectedEdgeBreakerMethod == -1)
                {
                    if (isStandardEdgeBreakerAvailable && (config.Speed >= 5 || !isPredictiveEdgeBreakerAvailable || isTinyMesh))
                    {
                        selectedEdgeBreakerMethod = Constants.EdgeBreakerTraversalDecoderType.StandardEdgeBreaker;
                    }
                    else
                    {
                        selectedEdgeBreakerMethod = Constants.EdgeBreakerTraversalDecoderType.ValenceEdgeBreaker;
                    }
                }

                encoderBuffer.WriteByte((byte)selectedEdgeBreakerMethod);
                if (selectedEdgeBreakerMethod == Constants.EdgeBreakerTraversalDecoderType.StandardEdgeBreaker)
                {
                    if (isStandardEdgeBreakerAvailable)
                    {
                        return new MeshEdgeBreakerTraversalEncoder(config);
                    }
                }
                else if (selectedEdgeBreakerMethod == Constants.EdgeBreakerTraversalDecoderType.ValenceEdgeBreaker)
                {
                    return new MeshEdgeBreakerTraversalValenceEncoder(config);
                }
                else if (selectedEdgeBreakerMethod == Constants.EdgeBreakerTraversalDecoderType.PredictiveEdgeBreaker)
                {
                    return new MeshEdgeBreakerTraversalPredictiveEncoder(config);
                }
            }
            else
            {
                return new MeshSequentialEncoder(config);
            }
        }
        throw new NotImplementedException("The given method is not supported.");
    }
}
