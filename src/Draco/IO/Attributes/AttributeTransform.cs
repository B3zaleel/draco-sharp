using Draco.IO.Enums;

namespace Draco.IO.Attributes;

internal abstract class AttributeTransform
{
    public abstract AttributeTransformType Type { get; }

    public virtual void Init(PointAttribute attribute) { }

    public void TransferToAttribute(PointAttribute attribute)
    {
        var transformData = new AttributeTransformData();
        CopyToAttributeTransformData(transformData);
        attribute.AttributeTransformData = transformData;
    }

    public PointAttribute InitTransformedAttribute(PointAttribute srcAttribute, int numEntries)
    {
        var numComponents = GetTransformedNumComponents(srcAttribute);
        var dataType = GetTransformedDataType(srcAttribute);
        var geometryAttribute = new GeometryAttribute(srcAttribute.AttributeType, null, (byte)numComponents, dataType, false, numComponents * Constants.DataTypeLength(dataType), 0);
        var transformedAttribute = new PointAttribute(geometryAttribute);
        transformedAttribute.Reset(numEntries);
        transformedAttribute.SetIdentityMapping();
        transformedAttribute.UniqueId = srcAttribute.UniqueId;
        return transformedAttribute;
    }

    public abstract void CopyToAttributeTransformData(AttributeTransformData data);
    public abstract void TransformAttribute(PointAttribute attribute, List<uint> pointIds, PointAttribute targetAttribute);
    public abstract void InverseTransformAttribute(PointAttribute attribute, PointAttribute targetAttribute);
    public abstract void DecodeParameters(DecoderBuffer decoderBuffer, PointAttribute targetAttribute);
    public abstract void EncodeParameters(EncoderBuffer encoderBuffer);
    public abstract DataType GetTransformedDataType(PointAttribute attribute);
    public abstract int GetTransformedNumComponents(PointAttribute attribute);
}
