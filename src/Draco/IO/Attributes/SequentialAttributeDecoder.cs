using Draco.IO.Attributes.PredictionSchemes;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes;

internal class SequentialAttributeDecoder
{
    public PointAttribute? PortableAttribute { get; set; }
    public PointAttribute? Attribute { get; private set; }
    public int AttributeId { get; private set; } = -1;
    public ConnectivityDecoder? ConnectivityDecoder { get; private set; }

    public SequentialAttributeDecoder() { }

    public SequentialAttributeDecoder(ConnectivityDecoder connectivityDecoder, int attributeId)
    {
        Init(connectivityDecoder, attributeId);
    }

    public virtual void Init(ConnectivityDecoder connectivityDecoder, int attributeId)
    {
        ConnectivityDecoder = connectivityDecoder;
        Attribute = connectivityDecoder.PointCloud?.GetAttributeById(attributeId);
        AttributeId = attributeId;
    }

    public void DecodePortableAttribute(DecoderBuffer decoderBuffer, List<uint> pointIds)
    {
        Assertions.ThrowIf(Attribute?.NumComponents <= 0);
        Attribute!.Reset(pointIds.Count);
        DecodeValues(decoderBuffer, pointIds);
    }

    public virtual void DecodeDataNeededByPortableTransform(DecoderBuffer decoderBuffer, List<uint> pointIds) { }

    public virtual void TransformAttributeToOriginalFormat(List<uint> pointIds) { }

    public PointAttribute? GetPortableAttribute()
    {
        if (Attribute!.IsMappingIdentity && PortableAttribute != null && PortableAttribute!.IsMappingIdentity)
        {
            PortableAttribute.SetExplicitMapping(Attribute.IndicesMapSize);

            for (uint i = 0; i < Attribute.IndicesMapSize; ++i)
            {
                PortableAttribute.SetPointMapEntry(i, Attribute.MappedIndex(i));
            }
        }
        return PortableAttribute;
    }

    public void InitPredictionScheme(DecoderBuffer decoderBuffer, IPredictionScheme predictionScheme)
    {
        for (int i = 0; i < predictionScheme.ParentAttributesCount; ++i)
        {
            var attributeId = ConnectivityDecoder!.PointCloud!.GetNamedAttributeId(predictionScheme.GetParentAttributeType(i));
            if (decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 0))
            {
                predictionScheme.ParentAttribute = ConnectivityDecoder.PointCloud.GetAttributeById(attributeId)!;
            }
            else
            {
                var pointAttribute = ConnectivityDecoder.GetPortableAttribute(attributeId);
                predictionScheme.ParentAttribute = pointAttribute!;
            }
        }
    }

    protected virtual void DecodeValues(DecoderBuffer decoderBuffer, List<uint> pointIds)
    {
        for (int i = 0; i < pointIds.Count; ++i)
        {
            var valueData = decoderBuffer.ReadBytes((int)Attribute!.ByteStride);
            Attribute.Buffer!.Write(valueData);
        }
    }
}
