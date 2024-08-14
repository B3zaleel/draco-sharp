using Draco.IO.Attributes.PredictionSchemes;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes;

internal class SequentialAttributeDecoder
{
    private PointAttribute? _portableAttribute;
    public PointAttribute? PortableAttribute
    {
        get
        {
            if (!Attribute!.IsMappingIdentity && _portableAttribute != null && _portableAttribute!.IsMappingIdentity)
            {
                _portableAttribute.SetExplicitMapping(Attribute.IndicesMapSize);

                for (uint i = 0; i < Attribute.IndicesMapSize; ++i)
                {
                    _portableAttribute.SetPointMapEntry(i, Attribute.MappedIndex(i));
                }
            }
            return _portableAttribute;
        }
        set
        {
            _portableAttribute = value;
        }
    }
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

    public void InitPredictionScheme(DecoderBuffer decoderBuffer, IPredictionScheme predictionScheme)
    {
        for (int i = 0; i < predictionScheme.ParentAttributesCount; ++i)
        {
            var attributeId = ConnectivityDecoder!.PointCloud!.GetNamedAttributeId(predictionScheme.GetParentAttributeType(i));
            if (decoderBuffer.BitStreamVersion < Constants.BitStreamVersion(2, 0))
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
        var bytePosition = 0;
        var entrySize = (int)Attribute!.ByteStride;

        for (int i = 0; i < pointIds.Count; ++i)
        {
            var valueData = decoderBuffer.ReadBytes(entrySize);
            Attribute.Buffer!.Write(valueData, bytePosition);
            bytePosition += entrySize;
        }
    }
}
