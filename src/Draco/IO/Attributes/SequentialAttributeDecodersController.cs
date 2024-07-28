namespace Draco.IO.Attributes;

internal class SequentialAttributeDecodersController : AttributesDecoder
{
    private readonly List<uint> _pointIds = [];
    private readonly PointsSequencer _sequencer;
    private List<SequentialAttributeDecoder> _sequentialDecoders = [];

    public SequentialAttributeDecodersController(PointsSequencer sequencer, ConnectivityDecoder? connectivityDecoder, PointCloud.PointCloud? pointCloud) : base(connectivityDecoder, pointCloud)
    {
        _sequencer = sequencer;
    }

    public override void DecodeAttributesData(DecoderBuffer decoderBuffer)
    {
        base.DecodeAttributesData(decoderBuffer);
        _sequentialDecoders = new List<SequentialAttributeDecoder>(AttributesCount);

        for (int i = 0; i < AttributesCount; ++i)
        {
            var decoderType = decoderBuffer.ReadByte();
            _sequentialDecoders.Add(CreateSequentialDecoder(decoderType));
            _sequentialDecoders[i].Init(ConnectivityDecoder!, GetAttributeId(i));
        }
    }

    public override void DecodeAttributes(DecoderBuffer decoderBuffer)
    {
        _sequencer.GenerateSequence(_pointIds);
        for (int i = 0; i < AttributesCount; ++i)
        {
            var pointAttribute = ConnectivityDecoder!.PointCloud?.GetAttributeById(GetAttributeId(i));
            _sequencer.UpdatePointToAttributeIndexMapping(pointAttribute!);
        }
        base.DecodeAttributes(decoderBuffer);
    }

    public new PointAttribute? GetPortableAttribute(int pointAttributeId)
    {
        var id = GetLocalIdForPointAttribute(pointAttributeId);
        return id < 0 || id >= _sequentialDecoders.Count ? null : _sequentialDecoders[id].GetPortableAttribute();
    }

    public override void DecodePortableAttributes(DecoderBuffer decoderBuffer)
    {
        for (int i = 0; i < AttributesCount; ++i)
        {
            _sequentialDecoders[i].DecodePortableAttribute(decoderBuffer, _pointIds);
        }
    }

    public override void DecodeDataNeededByPortableTransforms(DecoderBuffer decoderBuffer)
    {
        for (int i = 0; i < AttributesCount; ++i)
        {
            _sequentialDecoders[i].DecodeDataNeededByPortableTransform(decoderBuffer, _pointIds);
        }
    }

    public override void TransformAttributesToOriginalFormat()
    {
        for (int i = 0; i < AttributesCount; ++i)
        {
            _sequentialDecoders[i].TransformAttributeToOriginalFormat(_pointIds);
        }
    }

    public static SequentialAttributeDecoder CreateSequentialDecoder(uint decoderType)
    {
        return decoderType switch
        {
            (uint)SequentialAttributeEncoderType.Generic => new SequentialAttributeDecoder(),
            (uint)SequentialAttributeEncoderType.Integer => new SequentialIntegerAttributeDecoder(),
            (uint)SequentialAttributeEncoderType.Quantization => new SequentialQuantizationAttributeDecoder(),
            // (uint)SequentialAttributeEncoderType.Normals => new SequentialNormalAttributeDecoder(),
            _ => throw new NotImplementedException($"Cannot generate a sequential decoder for the decoder type {decoderType}"),
        };
    }
}
