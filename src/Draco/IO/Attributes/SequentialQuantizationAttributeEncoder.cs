using Draco.IO.Enums;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes;

internal class SequentialQuantizationAttributeEncoder : SequentialIntegerAttributeEncoder
{
    private readonly AttributeQuantizationTransform _quantizationTransform;
    public override byte UniqueId { get => (byte)SequentialAttributeEncoderType.Quantization; }

    public SequentialQuantizationAttributeEncoder(ConnectivityEncoder connectivityEncoder, int attributeId) : base(connectivityEncoder, attributeId)
    {
        var attribute = connectivityEncoder.PointCloud!.GetAttributeById(attributeId);
        _quantizationTransform = new();
        var quantizationBits = connectivityEncoder.Config.GetAttributeOption(attributeId, ConfigOptionName.Attribute.QuantizationBits, -1);
        Assertions.ThrowIf(attribute.DataType != DataType.Float32);
        Assertions.ThrowIf(quantizationBits < 1);

        if (connectivityEncoder.Config.IsAttributeOptionSet(attributeId, ConfigOptionName.Attribute.QuantizationOrigin) && connectivityEncoder.Config.IsAttributeOptionSet(attributeId, ConfigOptionName.Attribute.QuantizationRange))
        {
            var quantizationOrigin = new List<float>(connectivityEncoder.Config.GetAttributeOptionValues<float>(attributeId, ConfigOptionName.Attribute.QuantizationOrigin, attribute.NumComponents));
            var range = connectivityEncoder.Config.GetAttributeOption(attributeId, ConfigOptionName.Attribute.QuantizationRange, 1.0f);
            _quantizationTransform.SetParameters(quantizationBits, quantizationOrigin, attribute.NumComponents, range);
        }
        else
        {
            _quantizationTransform.ComputeParameters(attribute, quantizationBits);
        }
    }

    public override void EncodeDataNeededByPortableTransform(EncoderBuffer encoderBuffer)
    {
        _quantizationTransform.EncodeParameters(encoderBuffer);
    }

    protected override void PrepareValues(List<uint> pointIds, int numPoints)
    {
        var portableAttribute = _quantizationTransform.InitTransformedAttribute(Attribute!, pointIds.Count);
        _quantizationTransform.TransformAttribute(Attribute!, pointIds, portableAttribute);
        PortableAttribute = portableAttribute;
    }
}
