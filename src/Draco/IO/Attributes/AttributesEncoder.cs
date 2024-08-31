using Draco.IO.Extensions;

namespace Draco.IO.Attributes;

internal abstract class AttributesEncoder(ConnectivityEncoder? connectivityEncoder, PointCloud.PointCloud? pointCloud)
{
    protected List<int> _pointAttributeIds = [];
    protected List<int> _pointAttributeToLocalIdMap = [];
    protected ConnectivityEncoder? ConnectivityEncoder { get; private set; } = connectivityEncoder;
    protected PointCloud.PointCloud? PointCloud { get; private set; } = pointCloud;
    public int AttributesCount { get => _pointAttributeIds.Count; }

    public AttributesEncoder(ConnectivityEncoder? connectivityEncoder, PointCloud.PointCloud? pointCloud, int pointAttributeId) : this(connectivityEncoder, pointCloud)
    {
        AddAttributeId(pointAttributeId);
    }

    public virtual void EncodeAttributesData(EncoderBuffer encoderBuffer)
    {
        encoderBuffer.EncodeVarIntUnsigned((ulong)AttributesCount);

        for (uint i = 0; i < AttributesCount; ++i)
        {
            var attributeId = _pointAttributeIds[(int)i];
            var pointAttribute = PointCloud!.GetAttributeById(attributeId);
            encoderBuffer.WriteByte((byte)pointAttribute.AttributeType);
            encoderBuffer.WriteByte((byte)pointAttribute.DataType);
            encoderBuffer.WriteByte(pointAttribute.NumComponents);
            encoderBuffer.WriteByte((byte)(pointAttribute.Normalized ? 1 : 0));
            encoderBuffer.EncodeVarInt(pointAttribute.UniqueId);
        }
    }

    public virtual void EncodeAttributes(EncoderBuffer encoderBuffer)
    {
        TransformAttributesToPortableFormat();
        EncodePortableAttributes(encoderBuffer);
        EncodeDataNeededByPortableTransforms(encoderBuffer);
    }

    public int GetLocalIdForPointAttribute(int pointAttributeId)
    {
        return pointAttributeId < 0 || pointAttributeId >= _pointAttributeToLocalIdMap.Count ? -1 : _pointAttributeToLocalIdMap[pointAttributeId];
    }

    public virtual PointAttribute? GetPortableAttribute(int pointAttributeId)
    {
        return null;
    }

    public virtual int NumParentAttributes(int pointAttributeId)
    {
        return 0;
    }

    public virtual int GetParentAttributeId(int pointAttributeId, int parentId)
    {
        return -1;
    }

    public virtual void MarkParentAttribute(int pointAttributeId)
    {
    }

    public void AddAttributeId(int id)
    {
        _pointAttributeIds.Add(id);
        if (id >= _pointAttributeToLocalIdMap.Count)
        {
            _pointAttributeToLocalIdMap.Resize(id + 1, -1);
        }
        _pointAttributeToLocalIdMap[id] = _pointAttributeIds.Count - 1;
    }

    public void SetAttributeIds(List<int> pointAttributeIds)
    {
        _pointAttributeIds.Clear();
        _pointAttributeToLocalIdMap.Clear();
        for (int i = 0; i < pointAttributeIds.Count; ++i)
        {
            AddAttributeId(pointAttributeIds[i]);
        }
    }

    public int GetAttributeId(int i)
    {
        return _pointAttributeIds[i];
    }

    public abstract void EncodePortableAttributes(EncoderBuffer encoderBuffer);
    public abstract void EncodeDataNeededByPortableTransforms(EncoderBuffer encoderBuffer);
    public abstract void TransformAttributesToPortableFormat();
}
