using Draco.IO.Core;
using Draco.IO.Enums;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes;

internal class AttributeQuantizationTransform : AttributeTransform
{
    public int QuantizationBits { get; private set; } = -1;
    public List<float> MinValues { get; private set; } = [];
    public float Range { get; private set; } = 0.0f;
    public bool IsInitialized { get => QuantizationBits != -1; }
    public override AttributeTransformType Type { get => AttributeTransformType.QuantizationTransform; }

    public override void Init(PointAttribute attribute)
    {
        Assertions.ThrowIf(attribute.AttributeTransformData == null || attribute.AttributeTransformData.TransformType != AttributeTransformType.QuantizationTransform, "Wrong transform type.");
        var byteOffset = 0;
        QuantizationBits = attribute.AttributeTransformData!.GetParameterValue<int>(byteOffset);
        MinValues = [];
        byteOffset += 4;

        for (int i = 0; i < attribute.NumComponents; ++i)
        {
            MinValues.Add(attribute.AttributeTransformData.GetParameterValue<float>(byteOffset));
            byteOffset += 4;
        }
        Range = attribute.AttributeTransformData.GetParameterValue<float>(byteOffset);
    }

    private static bool IsQuantizationValid(int quantizationBits)
    {
        return quantizationBits >= 1 && quantizationBits <= 30;
    }

    public override DataType GetTransformedDataType(PointAttribute attribute)
    {
        return DataType.UInt32;
    }

    public override int GetTransformedNumComponents(PointAttribute attribute)
    {
        return attribute.NumComponents;
    }

    public override void CopyToAttributeTransformData(AttributeTransformData data)
    {
        data.TransformType = AttributeTransformType.OctahedronTransform;
        data.AppendParameterValue(QuantizationBits);

        for (int i = 0; i < MinValues.Count; ++i)
        {
            data.AppendParameterValue(MinValues[i]);
        }
        data.AppendParameterValue(Range);
    }

    public override void DecodeParameters(DecoderBuffer decoderBuffer, PointAttribute targetAttribute)
    {
        MinValues = [];

        for (int i = 0; i < targetAttribute.NumComponents; ++i)
        {
            MinValues.Add(decoderBuffer.ReadSingle());
        }
        Range = decoderBuffer.ReadSingle();
        QuantizationBits = decoderBuffer.ReadByte();
        Assertions.ThrowIfNot(IsQuantizationValid(QuantizationBits));
    }

    public override void TransformAttribute(PointAttribute attribute, List<uint> pointIds, PointAttribute targetAttribute)
    {
        Assertions.ThrowIfNot(IsInitialized);
        var numComponents = attribute.NumComponents;
        var maxQuantizedValue = (1 << QuantizationBits) - 1;
        var quantizer = new Quantizer(Range, maxQuantizedValue);
        int dstIndex = 0;
        float[] attValue;

        if (pointIds.Count == 0)
        {
            for (uint i = 0; i < targetAttribute.UniqueEntriesCount; ++i)
            {
                var attValueId = attribute.MappedIndex(i);
                attValue = attribute.GetValue<float>(attValueId, numComponents);

                for (int c = 0; c < numComponents; ++c)
                {
                    var value = attValue[c] - MinValues[c];
                    var qVal = quantizer.QuantizeFloat(value);
                    targetAttribute.Buffer!.Write(qVal, dstIndex);
                    dstIndex += Constants.SizeOf<int>();
                }
            }
        }
        else
        {
            for (uint i = 0; i < pointIds.Count; ++i)
            {
                var attValId = attribute.MappedIndex(pointIds[(int)i]);
                attValue = attribute.GetValue<float>(attValId, numComponents);

                for (int c = 0; c < numComponents; ++c)
                {
                    var value = attValue[c] - MinValues[c];
                    var qVal = quantizer.QuantizeFloat(value);
                    targetAttribute.Buffer!.Write(qVal, dstIndex);
                    dstIndex += Constants.SizeOf<int>();
                }
            }
        }
    }

    public override void InverseTransformAttribute(PointAttribute attribute, PointAttribute targetAttribute)
    {
        Assertions.ThrowIf(targetAttribute.DataType != DataType.Float32);
        var maxQuantizedValue = (int)((1U << QuantizationBits) - 1);
        int entrySize = sizeof(float) * targetAttribute.NumComponents;
        int bytePosition = 0;
        var dequantizer = new Dequantizer(Range, maxQuantizedValue);
        var attributeValue = new float[targetAttribute.NumComponents];
        var sourceAttributeDataPosition = attribute.GetAddress(0);

        for (uint i = 0; i < targetAttribute.UniqueEntriesCount; ++i)
        {
            for (int c = 0; c < targetAttribute.NumComponents; ++c)
            {
                attributeValue[c] = dequantizer.DequantizeFloat(attribute.Buffer!.Read<int>(sourceAttributeDataPosition)) + MinValues[c];
                sourceAttributeDataPosition += Constants.SizeOf<int>();
            }
            targetAttribute.Buffer!.Write(attributeValue, bytePosition);
            bytePosition += entrySize;
        }
    }
}
