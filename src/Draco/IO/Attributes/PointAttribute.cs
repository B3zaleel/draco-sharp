using Draco.IO.Extensions;

namespace Draco.IO.Attributes;

public class PointAttribute : GeometryAttribute
{
    public uint Size { get; private set; } = 0;
    private readonly List<uint> _indicesMap = [];

    public bool IsMappingIdentity { get; private set; } = false;
    public int IndicesMapSize { get => IsMappingIdentity ? 0 : _indicesMap.Count; }

    /// <summary>
    /// Contains the type and parameters of the transform that is applied on the attribute data.
    /// </summary>
    /// <value></value>
    public AttributeTransformData? AttributeTransformData { get; set; }

    public PointAttribute() { }

    public PointAttribute(GeometryAttribute att)
    {
        AttributeType = att.AttributeType;
        Normalized = att.Normalized;
        ByteOffset = att.ByteOffset;
        UniqueId = att.UniqueId;
    }

    public uint MappedIndex(uint pointIndex)
    {
        return IsMappingIdentity ? pointIndex : _indicesMap[(int)pointIndex];
    }

    public void Reset(int numAttributeValues)
    {
        if (Buffer == null)
        {
            Buffer = new MemoryStream();
        }
        ResetBuffer(Buffer, Constants.DataTypeLength(DataType) * NumComponents, 0);
        Size = (uint)numAttributeValues;
    }

    public void SetIdentityMapping()
    {
        IsMappingIdentity = true;
        _indicesMap.Clear();
    }

    public void SetExplicitMapping(int numPoints)
    {
        IsMappingIdentity = false;
        _indicesMap.Resize(numPoints, Constants.kInvalidAttributeValueIndex);
    }

    public void SetPointMapEntry(uint pointIndex, uint entryIndex)
    {
        Assertions.ThrowIf(IsMappingIdentity);
        _indicesMap[(int)pointIndex] = entryIndex;
    }
}
