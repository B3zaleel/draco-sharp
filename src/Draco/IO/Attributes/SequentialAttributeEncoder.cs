using Draco.IO.Attributes.PredictionSchemes;
using Draco.IO.Enums;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes;

internal class SequentialAttributeEncoder
{
    protected PointAttribute? _portableAttribute;
    public PointAttribute? Attribute { get; private set; }
    public PointAttribute? PortableAttribute
    {
        get => _portableAttribute ?? Attribute;
        protected set => _portableAttribute = value;
    }
    public List<int> ParentAttributes { get; set; } = [];
    protected bool IsParentEncoder { get; private set; }
    public int AttributeId { get; private set; } = -1;
    public byte UniqueId { get => (byte)SequentialAttributeEncoderType.Generic; }
    public ConnectivityEncoder? ConnectivityEncoder { get; private set; }

    public SequentialAttributeEncoder(ConnectivityEncoder connectivityEncoder, int attributeId)
    {
        ConnectivityEncoder = connectivityEncoder;
        Attribute = connectivityEncoder.PointCloud?.GetAttributeById(attributeId);
        AttributeId = attributeId;
    }

    public void MarkParentAttribute()
    {
        IsParentEncoder = true;
    }

    public void EncodePortableAttribute(EncoderBuffer encoderBuffer, List<uint> pointIds)
    {
        EncodeValues(encoderBuffer, pointIds);
    }

    protected virtual void EncodeValues(EncoderBuffer encoderBuffer, List<uint> pointIds)
    {
        for (uint i = 0; i < pointIds.Count; ++i)
        {
            var entryId = Attribute!.MappedIndex(pointIds[(int)i]);
            encoderBuffer.WriteBytes(Attribute.GetValue<byte>(entryId, (int)Attribute!.ByteStride));
        }
    }

    protected virtual void InitPredictionScheme(IPredictionScheme predictionScheme)
    {
        for (int i = 0; i < predictionScheme.ParentAttributesCount; ++i)
        {
            int attributeId = ConnectivityEncoder!.PointCloud!.GetNamedAttributeId(predictionScheme.GetParentAttributeType(i));
            Assertions.ThrowIf(attributeId == -1, "Requested attribute does not exist.");
            ParentAttributes.Add(attributeId);
            ConnectivityEncoder.MarkParentAttribute(attributeId);
        }
    }

    protected virtual void SetPredictionSchemeParentAttributes(IPredictionScheme predictionScheme)
    {
        for (int i = 0; i < predictionScheme.ParentAttributesCount; ++i)
        {
            int attributeId = ConnectivityEncoder!.PointCloud!.GetNamedAttributeId(predictionScheme.GetParentAttributeType(i));
            Assertions.ThrowIf(attributeId == -1, "Requested attribute does not exist.");
            predictionScheme.ParentAttribute = ConnectivityEncoder.GetPortableAttribute(attributeId);
        }
    }

    public virtual void EncodeDataNeededByPortableTransform(EncoderBuffer encoderBuffer) { }

    public virtual void TransformAttributeToPortableFormat(List<uint> pointIds) { }
}
