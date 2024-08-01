using Draco.IO.Attributes.PredictionSchemes;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes;

internal class SequentialNormalAttributeDecoder : SequentialIntegerAttributeDecoder
{
    private AttributeOctahedronTransform? _octahedronTransform;

    public override void Init(ConnectivityDecoder connectivityDecoder, int attributeId)
    {
        base.Init(connectivityDecoder, attributeId);
        Assertions.ThrowIf(Attribute!.NumComponents != 3);
        Assertions.ThrowIf(Attribute.DataType != DataType.Float32);
        _octahedronTransform = new(Attribute);
    }

    protected override IPredictionSchemeDecoder<int>? CreatePredictionScheme(PredictionSchemeMethod method, PredictionSchemeTransformType transformType)
    {
        return transformType switch
        {
            PredictionSchemeTransformType.NormalOctahedron => (IPredictionSchemeDecoder<int>?)PredictionSchemeDecoderFactory.CreatePredictionSchemeForDecoder<int, PredictionSchemeNormalOctahedronDecodingTransform<int>>(method, AttributeId, ConnectivityDecoder!, new()),
            PredictionSchemeTransformType.NormalOctahedronCanonicalized => (IPredictionSchemeDecoder<int>?)PredictionSchemeDecoderFactory.CreatePredictionSchemeForDecoder<int, PredictionSchemeNormalOctahedronCanonicalizedDecodingTransform<int>>(method, AttributeId, ConnectivityDecoder!, new()),
            _ => null
        };
    }

    protected override void DecodeIntegerValues(DecoderBuffer decoderBuffer, List<uint> pointIds)
    {
        if (decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 0))
        {
            _octahedronTransform!.DecodeParameters(decoderBuffer, Attribute!);
        }
        base.DecodeIntegerValues(decoderBuffer, pointIds);
    }

    public override void DecodeDataNeededByPortableTransform(DecoderBuffer decoderBuffer, List<uint> pointIds)
    {
        if (decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 0))
        {
            _octahedronTransform!.DecodeParameters(decoderBuffer, PortableAttribute!);
        }
        _octahedronTransform!.TransferToAttribute(PortableAttribute!);
    }

    protected new void StoreValues(uint numPoints)
    {
        _octahedronTransform!.InverseTransformAttribute(PortableAttribute!, Attribute!);
    }
}
