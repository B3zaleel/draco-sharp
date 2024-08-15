using Draco.IO.Metadata;

namespace Draco;

public class DracoMetadata
{
    public List<MetadataElement> Attributes { get; set; } = [];
    public required MetadataElement File { get; set; }
}
