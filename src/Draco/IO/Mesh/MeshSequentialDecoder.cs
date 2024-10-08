using Draco.IO.Attributes;
using Draco.IO.Entropy;

namespace Draco.IO.Mesh;

internal class MeshSequentialDecoder : MeshDecoder
{
    public override void DecodeConnectivity(DecoderBuffer decoderBuffer)
    {
        uint numFaces, numPoints;

        if (decoderBuffer.BitStreamVersion < Constants.BitStreamVersion(2, 2))
        {
            numFaces = decoderBuffer.ReadUInt32();
            numPoints = decoderBuffer.ReadUInt32();
        }
        else
        {
            numFaces = (uint)decoderBuffer.DecodeVarIntUnsigned();
            numPoints = (uint)decoderBuffer.DecodeVarIntUnsigned();
        }
        byte connectivityMethod = decoderBuffer.ReadByte();
        Mesh = new Mesh();
        if (connectivityMethod == Constants.SequentialIndicesEncodingMethod.CompressedIndices)
        {
            DecodeAndDecompressIndices(decoderBuffer, numFaces);
        }
        else if (connectivityMethod == Constants.SequentialIndicesEncodingMethod.UncompressedIndices)
        {
            if (numPoints < 256)
            {
                for (uint i = 0; i < numFaces; ++i)
                {
                    var face = new int[3];
                    for (byte j = 0; j < 3; ++j)
                    {
                        face[j] = decoderBuffer.ReadByte();
                    }
                    Mesh.AddFace(face);
                }
            }
            else if (numPoints < (1 << 16))
            {
                for (uint i = 0; i < numFaces; ++i)
                {
                    var face = new int[3];
                    for (byte j = 0; j < 3; ++j)
                    {
                        face[j] = decoderBuffer.ReadUInt16();
                    }
                    Mesh.AddFace(face);
                }
            }
            else if (numPoints < (1 << 21) && decoderBuffer.BitStreamVersion >= Constants.BitStreamVersion(2, 2))
            {
                for (uint i = 0; i < numFaces; ++i)
                {
                    var face = new int[3];
                    for (byte j = 0; j < 3; ++j)
                    {
                        face[j] = (int)decoderBuffer.DecodeVarIntUnsigned();
                    }
                    Mesh.AddFace(face);
                }
            }
            else
            {
                for (uint i = 0; i < numFaces; ++i)
                {
                    var face = new int[3];
                    for (byte j = 0; j < 3; ++j)
                    {
                        face[j] = (int)decoderBuffer.ReadUInt32();
                    }
                    Mesh.AddFace(face);
                }
            }
        }
        else
        {
            throw new InvalidDataException($"Unsupported sequential connectivity method {connectivityMethod}.");
        }
    }

    private void DecodeAndDecompressIndices(DecoderBuffer decoderBuffer, uint numFaces)
    {
        SymbolDecoding.DecodeSymbols(decoderBuffer, numFaces * 3, 1, out uint[] indicesBuffer);
        int lastIndexValue = 0;
        int vertexIndex = 0;
        for (uint i = 0; i < numFaces; ++i)
        {
            var face = new int[3];
            for (byte j = 0; j < 3; ++j)
            {
                var encodedVal = indicesBuffer[vertexIndex++];
                int indexDiff = (int)(encodedVal >> 1);
                if ((encodedVal & 1) == 0)
                {
                    if (indexDiff > lastIndexValue)
                    {
                        throw new InvalidDataException("Subtracting indexDiff would result in a negative index.");
                    }
                    indexDiff = -indexDiff;
                }
                else
                {
                    if (indexDiff > (int.MaxValue - lastIndexValue))
                    {
                        throw new InvalidDataException("Adding indexDiff to lastIndexValue would overflow.");
                    }
                }
                int indexValue = indexDiff + lastIndexValue;
                face[j] = indexValue;
                lastIndexValue = indexValue;
            }
            Mesh!.AddFace(face);
        }
    }

    protected override void CreateAttributesDecoder(DecoderBuffer decoderBuffer, int attDecoderId)
    {
        SetAttributesDecoder(attDecoderId, new SequentialAttributeDecodersController(new LinearSequencer(Mesh!.PointsCount), this, PointCloud));
    }
}
