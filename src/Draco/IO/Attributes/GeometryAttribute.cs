namespace Draco.IO.Attributes;

public class GeometryAttribute
{
    public GeometryAttributeType AttributeType { get; set; }
    public bool Normalized { get; set; }
    public long ByteOffset { get; set; }
    public uint UniqueId { get; set; }
}
