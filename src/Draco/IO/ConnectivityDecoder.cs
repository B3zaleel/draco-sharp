using Draco.IO.Attributes;
using Draco.IO.Extensions;

namespace Draco.IO;

internal abstract class ConnectivityDecoder
{
    public int BitStreamVersion { get; set; }
    public int GeometryType { get; }
    public PointCloud.PointCloud? PointCloud { get; }
    public List<AttributesDecoder?> AttributesDecoders { get; } = [];
    public List<int> AttributeToDecoderMap { get; } = [];

    public abstract void DecodeConnectivity(DecoderBuffer decoderBuffer);

    public void DecodeAttributes(DecoderBuffer decoderBuffer)
    {
        var numAttributesDecoders = decoderBuffer.ReadByte();
        for (int i = 0; i < numAttributesDecoders; ++i)
        {
            CreateAttributesDecoder(decoderBuffer, i);
        }
        for (int i = 0; i < numAttributesDecoders; ++i)
        {
            AttributesDecoders[i]!.DecodeAttributesData(decoderBuffer);
        }
        for (int i = 0; i < numAttributesDecoders; ++i)
        {
            var numAttributes = AttributesDecoders[i]!.AttributesCount;
            for (int j = 0; j < numAttributes; ++j)
            {
                var attributeId = AttributesDecoders[i]!.GetAttributeId(j);
                if (attributeId >= AttributeToDecoderMap.Count)
                {
                    AttributeToDecoderMap.Resize(attributeId + 1, -1);
                }
                AttributeToDecoderMap[attributeId] = i;
            }
        }
        foreach (var attDecoder in AttributesDecoders)
        {
            attDecoder!.DecodeAttributes(decoderBuffer);
        }
    }

    public PointAttribute? GetPortableAttribute(int parentAttributeId)
    {
        if (parentAttributeId < 0 || parentAttributeId >= PointCloud!.AttributesCount)
        {
            return null;
        }
        int parentAttributeDecoderId = AttributeToDecoderMap[parentAttributeId];
        return AttributesDecoders[parentAttributeDecoderId]!.GetPortableAttribute(parentAttributeId);
    }

    public void SetAttributesDecoder(int attDecoderId, AttributesDecoder attributesDecoder)
    {
        Assertions.ThrowIf(attDecoderId < 0);
        if (attDecoderId >= AttributesDecoders.Count)
        {
            AttributesDecoders.Resize(attDecoderId + 1, () => null);
        }
        AttributesDecoders[attDecoderId] = attributesDecoder;
    }

    protected abstract void CreateAttributesDecoder(DecoderBuffer decoderBuffer, int attDecoderId);
}
