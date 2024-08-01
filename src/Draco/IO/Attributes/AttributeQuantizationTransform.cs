using Draco.IO.Core;
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
        QuantizationBits = attribute.AttributeTransformData!.GetParameterValue<int>();
        MinValues = [];

        for (int i = 0; i < attribute.NumComponents; ++i)
        {
            MinValues.Add(attribute.AttributeTransformData.GetParameterValue<float>());
        }
        Range = attribute.AttributeTransformData.GetParameterValue<float>();
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
        float[] attValue;

        if (pointIds.Count == 0)
        {
            for (uint i = 0; i < targetAttribute.Size; ++i)
            {
                var attValueId = attribute.MappedIndex(i);
                attValue = attribute.GetValue<float>(attValueId, numComponents);

                for (int c = 0; c < numComponents; ++c)
                {
                    var value = attValue[c] - MinValues[c];
                    var qVal = quantizer.QuantizeFloat(value);
                    targetAttribute.Buffer!.WriteDatum(qVal);
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
                    targetAttribute.Buffer!.WriteDatum(qVal);
                }
            }
        }
    }

    public override void InverseTransformAttribute(PointAttribute attribute, PointAttribute targetAttribute)
    {
        Assertions.ThrowIfNot(targetAttribute.DataType != DataType.Float32);
        var maxQuantizedValue = (int)((1U << QuantizationBits) - 1);
        var dequantizer = new Dequantizer(Range, maxQuantizedValue);
        var attributeValue = new float[targetAttribute.NumComponents];

        for (uint i = 0; i < targetAttribute.Size; ++i)
        {
            for (int c = 0; c < targetAttribute.NumComponents; ++c)
            {
                attributeValue[c] = dequantizer.DequantizeFloat(attribute.GetValue<int>()) + MinValues[c];
            }
            targetAttribute.Buffer!.WriteData(attributeValue);
        }
    }
}
