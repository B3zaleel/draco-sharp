using Draco.IO.Enums;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes;

internal class SequentialQuantizationAttributeDecoder : SequentialIntegerAttributeDecoder
{
    private AttributeQuantizationTransform? _quantizationTransform;

    public override void Init(ConnectivityDecoder connectivityDecoder, int attributeId)
    {
        base.Init(connectivityDecoder, attributeId);
        Assertions.ThrowIf(Attribute!.DataType != DataType.Float32);
        _quantizationTransform = new();
    }

    protected override void DecodeIntegerValues(DecoderBuffer decoderBuffer, List<uint> pointIds)
    {
        if (decoderBuffer.BitStreamVersion < Constants.BitStreamVersion(2, 0))
        {
            DecodeQuantizedDataInfo(decoderBuffer);
        }
        base.DecodeIntegerValues(decoderBuffer, pointIds);
    }

    public override void DecodeDataNeededByPortableTransform(DecoderBuffer decoderBuffer, List<uint> pointIds)
    {
        if (decoderBuffer.BitStreamVersion >= Constants.BitStreamVersion(2, 0))
        {
            DecodeQuantizedDataInfo(decoderBuffer);
        }
        _quantizationTransform!.TransferToAttribute(PortableAttribute!);
    }

    protected override void StoreValues(uint numPoints)
    {
        DequantizeValues(numPoints);
    }

    private void DecodeQuantizedDataInfo(DecoderBuffer decoderBuffer)
    {
        _quantizationTransform!.DecodeParameters(decoderBuffer, (PortableAttribute ?? Attribute)!);
    }

    public void DequantizeValues(uint numValues)
    {
        _quantizationTransform!.InverseTransformAttribute(PortableAttribute!, Attribute!);
    }
}
