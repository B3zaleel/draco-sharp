using Draco.IO.Enums;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes;

internal class SequentialAttributeEncodersController : AttributesEncoder
{
    private readonly List<uint> _pointIds = [];
    private readonly List<bool> _sequentialEncoderMarkedAsParent = [];
    private readonly PointsSequencer _sequencer;
    private List<SequentialAttributeEncoder> _sequentialEncoders = [];

    public SequentialAttributeEncodersController(PointsSequencer sequencer, ConnectivityEncoder? connectivityEncoder, PointCloud.PointCloud? pointCloud) : base(connectivityEncoder, pointCloud)
    {
        _sequencer = sequencer;
        CreateSequentialEncoders(connectivityEncoder!);
    }

    public SequentialAttributeEncodersController(PointsSequencer sequencer, int pointAttributeId, ConnectivityEncoder? connectivityEncoder, PointCloud.PointCloud? pointCloud) : base(connectivityEncoder, pointCloud)
    {
        _sequencer = sequencer;
        AddAttributeId(pointAttributeId);
        CreateSequentialEncoders(connectivityEncoder!);
    }

    public override void EncodeAttributesData(EncoderBuffer encoderBuffer)
    {
        base.EncodeAttributesData(encoderBuffer);

        for (int i = 0; i < _sequentialEncoders.Count; ++i)
        {
            encoderBuffer.WriteByte(_sequentialEncoders[i].UniqueId);
        }
    }

    public override void EncodeAttributes(EncoderBuffer encoderBuffer)
    {
        _sequencer.GenerateSequence(_pointIds);
        base.EncodeAttributes(encoderBuffer);
    }

    public override PointAttribute? GetPortableAttribute(int pointAttributeId)
    {
        var localId = GetLocalIdForPointAttribute(pointAttributeId);
        return localId < 0 ? null : _sequentialEncoders[localId].PortableAttribute;
    }

    public override int NumParentAttributes(int pointAttributeId)
    {
        var localId = GetLocalIdForPointAttribute(pointAttributeId);
        return localId < 0 ? 0 : _sequentialEncoders[localId].ParentAttributes.Count;
    }

    public override int GetParentAttributeId(int pointAttributeId, int parentId)
    {
        var localId = GetLocalIdForPointAttribute(pointAttributeId);
        return localId < 0 ? -1 : _sequentialEncoders[localId].ParentAttributes[parentId];
    }

    public override void MarkParentAttribute(int pointAttributeId)
    {
        var localId = GetLocalIdForPointAttribute(pointAttributeId);
        if (localId < 0)
        {
            return;
        }
        if (_sequentialEncoderMarkedAsParent.Count <= localId)
        {
            _sequentialEncoderMarkedAsParent.Resize(localId + 1, false);
        }
        _sequentialEncoderMarkedAsParent[localId] = true;
        if (_sequentialEncoders.Count <= localId)
        {
            return;
        }
        _sequentialEncoders[localId].MarkParentAttribute();
    }

    public override void EncodePortableAttributes(EncoderBuffer encoderBuffer)
    {
        for (int i = 0; i < _sequentialEncoders.Count; ++i)
        {
            _sequentialEncoders[i].EncodePortableAttribute(encoderBuffer, _pointIds);
        }
    }

    public override void EncodeDataNeededByPortableTransforms(EncoderBuffer encoderBuffer)
    {
        for (int i = 0; i < _sequentialEncoders.Count; ++i)
        {
            _sequentialEncoders[i].EncodeDataNeededByPortableTransform(encoderBuffer);
        }
    }

    public override void TransformAttributesToPortableFormat()
    {
        for (int i = 0; i < _sequentialEncoders.Count; ++i)
        {
            _sequentialEncoders[i].TransformAttributeToPortableFormat(_pointIds);
        }
    }

    public void CreateSequentialEncoders(ConnectivityEncoder connectivityEncoder)
    {
        _sequentialEncoders = new List<SequentialAttributeEncoder>(AttributesCount);
        for (int i = 0; i < AttributesCount; ++i)
        {
            _sequentialEncoders.Add(CreateSequentialEncoder(connectivityEncoder, i));
            if (i < _sequentialEncoderMarkedAsParent.Count)
            {
                if (_sequentialEncoderMarkedAsParent[i])
                {
                    _sequentialEncoders[i].MarkParentAttribute();
                }
            }
        }
    }

    private SequentialAttributeEncoder CreateSequentialEncoder(ConnectivityEncoder connectivityEncoder, int i)
    {
        int attributeId = GetAttributeId(i);
        var attribute = ConnectivityEncoder!.PointCloud!.GetAttributeById(attributeId);

        switch (attribute.DataType)
        {
            case DataType.UInt8:
            case DataType.Int8:
            case DataType.UInt16:
            case DataType.Int16:
            case DataType.UInt32:
            case DataType.Int32:
                return new SequentialIntegerAttributeEncoder(connectivityEncoder, attributeId);
            case DataType.Float32:
                {
                    if (ConnectivityEncoder.Config.GetAttributeOption(attributeId, ConfigOptionName.Attribute.QuantizationBits, -1) > 0)
                    {
                        return attribute.AttributeType == GeometryAttributeType.Normal
                            ? new SequentialNormalAttributeEncoder(connectivityEncoder, attributeId)
                            : new SequentialQuantizationAttributeEncoder(connectivityEncoder, attributeId);
                    }
                    break;
                }
            default:
                break;
        }
        return new SequentialAttributeEncoder(connectivityEncoder, attributeId);
    }
}
