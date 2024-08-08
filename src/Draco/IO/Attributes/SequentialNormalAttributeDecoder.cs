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
        _octahedronTransform = new();
    }

    protected override IPredictionSchemeDecoder<int>? CreatePredictionScheme(PredictionSchemeMethod method, PredictionSchemeTransformType transformType)
    {
        return transformType switch
        {
            PredictionSchemeTransformType.NormalOctahedron => PredictionSchemeDecoderFactory.CreatePredictionSchemeForDecoder<int, PredictionSchemeDecodingTransform<int, int>>(method, AttributeId, ConnectivityDecoder!, new PredictionSchemeNormalOctahedronDecodingTransform<int>()),
            PredictionSchemeTransformType.NormalOctahedronCanonicalized => PredictionSchemeDecoderFactory.CreatePredictionSchemeForDecoder<int, PredictionSchemeDecodingTransform<int, int>>(method, AttributeId, ConnectivityDecoder!, new PredictionSchemeNormalOctahedronDecodingTransform<int>()),
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

    protected override void StoreValues(uint numPoints)
    {
        _octahedronTransform!.InverseTransformAttribute(PortableAttribute!, Attribute!);
    }
}
