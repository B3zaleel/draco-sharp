using Draco.IO.Enums;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes;

internal class AttributeOctahedronTransform : AttributeTransform
{
    private int _quantizationBits = -1;

    public override AttributeTransformType Type { get => AttributeTransformType.OctahedronTransform; }

    public override void Init(PointAttribute attribute)
    {
        Assertions.ThrowIf(attribute.AttributeTransformData == null || attribute.AttributeTransformData.TransformType != AttributeTransformType.OctahedronTransform, "Wrong transform type.");
        _quantizationBits = attribute.AttributeTransformData!.GetParameterValue<int>(0);
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

    public override void EncodeParameters(EncoderBuffer encoderBuffer)
    {
        encoderBuffer.WriteByte((byte)_quantizationBits);
    }

    public override void TransformAttribute(PointAttribute attribute, List<uint> pointIds, PointAttribute targetAttribute)
    {
        var portableAttributeDataPosition = targetAttribute.GetAddress(0);
        double[] attributeValue;
        var converter = new OctahedronToolBox();
        converter.SetQuantizationBits((byte)_quantizationBits);

        if (pointIds.Count == 0)
        {
            for (uint i = 0; i < targetAttribute.UniqueEntriesCount; ++i)
            {
                attributeValue = attribute.GetValue<float>(attribute.MappedIndex(i), 3).Select(value => (double)value).ToArray();
                var (s, t) = converter.FloatVectorToQuantizedOctahedralCoords(attributeValue);
                targetAttribute.Buffer!.Write(s, portableAttributeDataPosition);
                portableAttributeDataPosition += Constants.SizeOf<int>();
                targetAttribute.Buffer!.Write(t, portableAttributeDataPosition);
                portableAttributeDataPosition += Constants.SizeOf<int>();
            }
        }
        else
        {
            for (uint i = 0; i < pointIds.Count; ++i)
            {
                attributeValue = attribute.GetValue<float>(attribute.MappedIndex(pointIds[(int)i]), 3).Select(value => (double)value).ToArray();
                var (s, t) = converter.FloatVectorToQuantizedOctahedralCoords(attributeValue);
                targetAttribute.Buffer!.Write(s, portableAttributeDataPosition);
                portableAttributeDataPosition += Constants.SizeOf<int>();
                targetAttribute.Buffer.Write(t, portableAttributeDataPosition);
                portableAttributeDataPosition += Constants.SizeOf<int>();
            }
        }
    }

    public override void InverseTransformAttribute(PointAttribute attribute, PointAttribute targetAttribute)
    {
        Assertions.ThrowIfNot(targetAttribute.DataType != DataType.Float32);
        Assertions.ThrowIf(targetAttribute.NumComponents != 3);
        var entrySize = sizeof(float) * 3;
        var octahedronToolBox = new OctahedronToolBox();
        octahedronToolBox.SetQuantizationBits((byte)_quantizationBits);
        var sourceAttributeDataPosition = attribute.GetAddress(0);
        var targetAttributeDataPosition = targetAttribute.GetAddress(0);

        for (uint i = 0; i < targetAttribute.UniqueEntriesCount; ++i)
        {
            var s = attribute.Buffer!.Read<int>(sourceAttributeDataPosition);
            sourceAttributeDataPosition += Constants.SizeOf<int>();
            var t = attribute.Buffer!.Read<int>(sourceAttributeDataPosition);
            sourceAttributeDataPosition += Constants.SizeOf<int>();
            var attributeValue = octahedronToolBox.QuantizedOctahedralCoordsToUnitVector(s, t);
            targetAttribute.Buffer!.Write(attributeValue, targetAttributeDataPosition);
            targetAttributeDataPosition += entrySize;
        }
    }
}
