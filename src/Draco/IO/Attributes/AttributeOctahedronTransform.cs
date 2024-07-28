using Draco.IO.Extensions;

namespace Draco.IO.Attributes;

internal class AttributeOctahedronTransform : AttributeTransform
{
    private int _quantizationBits = -1;

    public override AttributeTransformType Type { get => AttributeTransformType.OctahedronTransform; }

    public AttributeOctahedronTransform(PointAttribute attribute)
    {
        Assertions.ThrowIf(attribute.AttributeTransformData == null || attribute.AttributeTransformData.TransformType != AttributeTransformType.OctahedronTransform, "Wrong transform type.");
        _quantizationBits = attribute.AttributeTransformData!.GetParameterValue<int>();
    }

    public override DataType GetTransformedDataType(PointAttribute attribute)
    {
        return DataType.UInt32;
    }

    public override int GetTransformedNumComponents(PointAttribute attribute)
    {
        return 2;
    }

    public override void CopyToAttributeTransformData(AttributeTransformData data)
    {
        data.TransformType = AttributeTransformType.OctahedronTransform;
        data.AppendParameterValue(_quantizationBits);
    }

    public void SetParameters(int quantizationBits)
    {
        _quantizationBits = quantizationBits;
    }

    public override void DecodeParameters(DecoderBuffer decoderBuffer, PointAttribute targetAttribute)
    {
        _quantizationBits = decoderBuffer.ReadByte();
    }

    public override void TransformAttribute(PointAttribute attribute, List<uint> pointIds, PointAttribute targetAttribute)
    {
        double[] attributeValue;
        var converter = new OctahedronToolBox();
        converter.SetQuantizationBits((byte)_quantizationBits);

        if (pointIds.Count == 0)
        {
            for (uint i = 0; i < targetAttribute.Size; ++i)
            {
                attributeValue = attribute.GetValue<float>(attribute.MappedIndex(i), 3).Select(value => (double)value).ToArray();
                var (s, t) = converter.FloatVectorToQuantizedOctahedralCoords(attributeValue);
                targetAttribute.Buffer!.WriteDatum(s);
                targetAttribute.Buffer!.WriteDatum(t);
            }
        }
        else
        {
            for (uint i = 0; i < pointIds.Count; ++i)
            {
                attributeValue = attribute.GetValue<float>(attribute.MappedIndex(pointIds[(int)i]), 3).Select(value => (double)value).ToArray();
                var (s, t) = converter.FloatVectorToQuantizedOctahedralCoords(attributeValue);
                targetAttribute.Buffer!.WriteDatum(s);
                targetAttribute.Buffer!.WriteDatum(t);
            }
        }
    }

    public override void InverseTransformAttribute(PointAttribute attribute, PointAttribute targetAttribute)
    {
        Assertions.ThrowIfNot(targetAttribute.DataType != DataType.Float32);
        Assertions.ThrowIf(targetAttribute.NumComponents != 3);
        var octahedronToolBox = new OctahedronToolBox();
        octahedronToolBox.SetQuantizationBits((byte)_quantizationBits);

        for (uint i = 0; i < targetAttribute.Size; ++i)
        {
            var s = attribute.Buffer!.ReadDatum<int>();
            var t = attribute.Buffer!.ReadDatum<int>();
            var attributeValue = octahedronToolBox.QuantizedOctahedralCoordsToUnitVector(s, t);
            targetAttribute.Buffer!.WriteData(attributeValue);
        }
    }
}
