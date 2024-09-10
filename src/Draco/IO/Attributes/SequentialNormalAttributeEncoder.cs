using Draco.IO.Attributes.PredictionSchemes;
using Draco.IO.Enums;

namespace Draco.IO.Attributes;

internal class SequentialNormalAttributeEncoder : SequentialIntegerAttributeEncoder
{
    private readonly AttributeOctahedronTransform _octahedronTransform;
    public override byte UniqueId { get => (byte)SequentialAttributeEncoderType.Normals; }

    public SequentialNormalAttributeEncoder(ConnectivityEncoder connectivityEncoder, int attributeId) : base(connectivityEncoder, attributeId)
    {
        var quantizationBits = connectivityEncoder.Config.GetAttributeOption(attributeId, ConfigOptionName.Attribute.QuantizationBits, -1);
        _octahedronTransform = new();
        _octahedronTransform.SetParameters(quantizationBits);
    }

    protected override IPredictionSchemeEncoder<int>? CreatePredictionScheme(PredictionSchemeMethod method)
    {
        var quantizationBits = ConnectivityEncoder!.Config.GetAttributeOption(AttributeId, ConfigOptionName.Attribute.QuantizationBits, -1);
        var maxValue = (1 << quantizationBits) - 1;
        var defaultPredictionMethod = PredictionSchemeEncoderFactory.SelectPredictionMethod(ConnectivityEncoder, AttributeId);
        var predictionMethod = ConnectivityEncoder.Config.GetAttributeOption(AttributeId, ConfigOptionName.Attribute.PredictionScheme, defaultPredictionMethod);

        return predictionMethod switch
        {
            PredictionSchemeMethod.GeometricNormal => PredictionSchemeEncoderFactory.CreatePredictionScheme<int, IPredictionSchemeEncodingTransform<int, int>>(method, AttributeId, ConnectivityEncoder, new PredictionSchemeNormalOctahedronCanonicalizedEncodingTransform<int>(maxValue)),
            PredictionSchemeMethod.Difference => PredictionSchemeEncoderFactory.CreatePredictionScheme<int, IPredictionSchemeEncodingTransform<int, int>>(method, AttributeId, ConnectivityEncoder, new PredictionSchemeNormalOctahedronCanonicalizedEncodingTransform<int>(maxValue)),
            _ => null
        };
    }

    public override void EncodeDataNeededByPortableTransform(EncoderBuffer encoderBuffer)
    {
        _octahedronTransform.EncodeParameters(encoderBuffer);
    }

    protected override void PrepareValues(List<uint> pointIds, int numPoints)
    {
        var portableAttribute = _octahedronTransform.InitTransformedAttribute(Attribute!, pointIds.Count);
        _octahedronTransform.TransformAttribute(Attribute!, pointIds, portableAttribute);
        PortableAttribute = portableAttribute;
    }
}
