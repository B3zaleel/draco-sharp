using Draco.IO;
using Draco.IO.Attributes;
using Draco.IO.Metadata;

namespace Draco;

/// <summary>
/// Represents the contents of a Draco stream.
/// </summary>
public class Draco
{
    public required DracoHeader Header { get; set; }
    public MetadataElement[]? AttMetadata { get; set; }
    public MetadataElement? FileMetadata { get; set; }
    public List<PointAttribute> Attributes { get; set; } = [];
}
