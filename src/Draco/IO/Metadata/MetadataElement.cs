namespace Draco.IO.Metadata;

public class MetadataElement
{
    public uint? Id { get; set; } = null;
    public sbyte[][] Keys { get; set; } = [];
    public sbyte[][] Values { get; set; } = [];
    public sbyte[][] SubMetadataKeys { get; set; } = [];
    public MetadataElement[]? SubMetadata { get; set; }
}
