using Draco.IO.Extensions;

namespace Draco.IO.Attributes;

internal abstract class AttributesDecoder(ConnectivityDecoder? connectivityDecoder, PointCloud.PointCloud? pointCloud)
{
    protected List<int> _pointAttributeIds = [];
    protected List<int> _pointAttributeToLocalIdMap = [];
    protected ConnectivityDecoder? ConnectivityDecoder { get; private set; } = connectivityDecoder;
    protected PointCloud.PointCloud? PointCloud { get; private set; } = pointCloud;
    public int AttributesCount { get => _pointAttributeIds.Count; }

    public int GetAttributeId(int id)
    {
        return _pointAttributeIds[id];
    }

    public virtual void DecodeAttributesData(DecoderBuffer decoderBuffer)
    {
        uint numAttributes = decoderBuffer.BitStreamVersion < Constants.BitStreamVersion(2, 0)
            ? decoderBuffer.ReadUInt32()
            : (uint)decoderBuffer.DecodeVarIntUnsigned();

        if (numAttributes == 0)
        {
            return;
        }
        _pointAttributeIds.Resize((int)numAttributes, 0);

        for (int i = 0; i < numAttributes; ++i)
        {
            var attributeType = decoderBuffer.ReadByte();
            var dataType = decoderBuffer.ReadByte();
            var numComponents = decoderBuffer.ReadByte();
            var normalized = decoderBuffer.ReadByte() != 0;
            Assertions.ThrowIf(attributeType >= (byte)GeometryAttributeType.NamedAttributesCount);
            Assertions.ThrowIf(dataType == (byte)DataType.Invalid || dataType >= (byte)DataType.Count);
            Assertions.ThrowIf(numComponents == 0);
            var geometryAttribute = new GeometryAttribute(
                attributeType: (GeometryAttributeType)attributeType,
                buffer: null,
                numComponents: numComponents,
                dataType: (DataType)dataType,
                normalized: normalized,
                byteStride: Constants.DataTypeLength((DataType)dataType) * numComponents,
                byteOffset: 0
            );
            uint uniqueId = decoderBuffer.BitStreamVersion < Constants.BitStreamVersion(1, 3)
                ? decoderBuffer.ReadUInt16()
                : (uint)decoderBuffer.DecodeVarIntUnsigned();
            geometryAttribute.UniqueId = uniqueId;
            var attributeId = PointCloud!.AddAttribute(new PointAttribute(geometryAttribute));
            PointCloud!.GetAttributeById(attributeId)!.UniqueId = uniqueId;
            _pointAttributeIds[i] = attributeId;

            if (attributeId >= _pointAttributeToLocalIdMap.Count)
            {
                _pointAttributeToLocalIdMap.Resize(attributeId + 1, -1);
            }
            _pointAttributeToLocalIdMap[attributeId] = i;
        }
    }

    public virtual void DecodeAttributes(DecoderBuffer decoderBuffer)
    {
        DecodePortableAttributes(decoderBuffer);
        DecodeDataNeededByPortableTransforms(decoderBuffer);
        TransformAttributesToOriginalFormat();
    }

    public int GetLocalIdForPointAttribute(int pointAttributeId)
    {
        return pointAttributeId < 0 || pointAttributeId >= _pointAttributeToLocalIdMap.Count ? -1 : _pointAttributeToLocalIdMap[pointAttributeId];
    }

    public virtual PointAttribute? GetPortableAttribute(int pointAttributeId)
    {
        return null;
    }

    public abstract void DecodePortableAttributes(DecoderBuffer decoderBuffer);
    public abstract void DecodeDataNeededByPortableTransforms(DecoderBuffer decoderBuffer);
    public abstract void TransformAttributesToOriginalFormat();
}
