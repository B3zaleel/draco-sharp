using Draco.IO.Attributes;
using Draco.IO.Entropy;
using Draco.IO.Enums;

namespace Draco.IO.Mesh;

internal class MeshSequentialEncoder(Config config) : MeshEncoder(config)
{
    public override void EncodeConnectivity(EncoderBuffer encoderBuffer)
    {
        encoderBuffer.EncodeVarInt(Mesh.FacesCount);
        encoderBuffer.EncodeVarInt(Mesh.PointsCount);

        if (Config.GetOption(ConfigOptionName.CompressConnectivity, false))
        {
            encoderBuffer.WriteByte(Constants.SequentialIndicesEncodingMethod.CompressedIndices);
            CompressAndEncodeIndices(encoderBuffer);
        }
        else
        {
            encoderBuffer.WriteByte(Constants.SequentialIndicesEncodingMethod.UncompressedIndices);
            if (Mesh.PointsCount < 256)
            {
                for (uint i = 0; i < Mesh.FacesCount; ++i)
                {
                    var face = Mesh.GetFace(i);
                    encoderBuffer.WriteByte((byte)face[0]);
                    encoderBuffer.WriteByte((byte)face[1]);
                    encoderBuffer.WriteByte((byte)face[2]);
                }
            }
            else if (Mesh.PointsCount < 65536)
            {
                for (uint i = 0; i < Mesh.FacesCount; ++i)
                {
                    var face = Mesh.GetFace(i);
                    encoderBuffer.WriteUInt16((ushort)face[0]);
                    encoderBuffer.WriteUInt16((ushort)face[1]);
                    encoderBuffer.WriteUInt16((ushort)face[2]);
                }
            }
            else if (Mesh.PointsCount < 2097152)
            {
                for (uint i = 0; i < Mesh.FacesCount; ++i)
                {
                    var face = Mesh.GetFace(i);
                    encoderBuffer.WriteUInt32((uint)face[0]);
                    encoderBuffer.WriteUInt32((uint)face[1]);
                    encoderBuffer.WriteUInt32((uint)face[2]);
                }
            }
            else
            {
                for (uint i = 0; i < Mesh.FacesCount; ++i)
                {
                    var face = Mesh.GetFace(i);
                    encoderBuffer.WriteUInt32((uint)face[0]);
                    encoderBuffer.WriteUInt32((uint)face[1]);
                    encoderBuffer.WriteUInt32((uint)face[2]);
                }
            }
        }
        throw new NotImplementedException();
    }

    private void CompressAndEncodeIndices(EncoderBuffer encoderBuffer)
    {
        var indicesBuffer = new List<uint>();
        var lastIndexValue = 0;

        for (uint i = 0; i < Mesh.FacesCount; ++i)
        {
            var face = Mesh.GetFace(i);

            for (byte j = 0; j < 3; ++j)
            {
                var indexValue = face[j];
                var indexDiff = indexValue - lastIndexValue;
                indicesBuffer.Add((uint)Math.Abs(indexDiff) << 1 | (indexDiff < 0 ? 1u : 0u));
                lastIndexValue = indexValue;
            }
        }
        SymbolEncoding.EncodeSymbols(encoderBuffer, null, indicesBuffer, 1);
    }

    public override void GenerateAttributesEncoder(int attributeId)
    {
        if (attributeId == 0)
        {
            AttributesEncoders.Add(new SequentialAttributeEncodersController(new LinearSequencer(Mesh.PointsCount), attributeId, this, Mesh));
        }
        else
        {
            AttributesEncoders[0].AddAttributeId(attributeId);
        }
    }

    public override void ComputeNumberOfEncodedPoints()
    {
        EncodedPointsCount = Mesh.PointsCount;
    }

    public override void ComputeNumberOfEncodedFaces()
    {
        EncodedFacesCount = Mesh.FacesCount;
    }
}
