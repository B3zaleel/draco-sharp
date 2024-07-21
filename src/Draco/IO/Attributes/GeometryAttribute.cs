using Draco.IO.Extensions;

namespace Draco.IO.Attributes;

public class GeometryAttribute
{
    public Stream? Buffer { get; private set; }
    public GeometryAttributeType AttributeType { get; set; }
    public byte NumComponents { get; set; }
    public DataType DataType { get; set; }
    public bool Normalized { get; set; }
    public long ByteStride { get; set; }
    public long ByteOffset { get; set; }
    public uint UniqueId { get; set; }

    internal GeometryAttribute() : this(GeometryAttributeType.Invalid, null, 1, DataType.Float32, false, 0, 0) { }

    internal GeometryAttribute(GeometryAttributeType attributeType, Stream? buffer, byte numComponents, DataType dataType, bool normalized, long byteStride, long byteOffset)
    {
        Buffer = buffer;
        AttributeType = attributeType;
        NumComponents = numComponents;
        DataType = dataType;
        Normalized = normalized;
        ByteStride = byteStride;
        ByteOffset = byteOffset;
    }

    public void CopyFrom(GeometryAttribute srcAttribute)
    {
        AttributeType = srcAttribute.AttributeType;
        NumComponents = srcAttribute.NumComponents;
        DataType = srcAttribute.DataType;
        Normalized = srcAttribute.Normalized;
        ByteStride = srcAttribute.ByteStride;
        ByteOffset = srcAttribute.ByteOffset;
        UniqueId = srcAttribute.UniqueId;

        if (srcAttribute.Buffer == null)
        {
            Buffer = null;
        }
        else
        {
            Buffer = new MemoryStream();
            Buffer.Update(srcAttribute.Buffer);
        }
    }

    public void ResetBuffer(Stream buffer, long byteStride, long byteOffset)
    {
        Buffer = buffer;
        ByteStride = byteStride;
        ByteOffset = byteOffset;
    }
}
