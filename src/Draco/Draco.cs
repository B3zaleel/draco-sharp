using Draco.IO.Attributes;

namespace Draco;

/// <summary>
/// Represents the contents of a Draco stream.
/// </summary>
public class Draco
{
    public required DracoHeader Header { get; set; }
    public DracoMetadata? Metadata { get; set; }
    public List<PointAttribute> Attributes { get; set; } = [];
}
