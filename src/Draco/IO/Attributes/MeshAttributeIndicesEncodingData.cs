using Draco.IO.Extensions;

namespace Draco.IO.Attributes;

public class MeshAttributeIndicesEncodingData
{
    /// <summary>
    /// Total number of encoded/decoded attribute entries.
    /// </summary>
    public int NumValues { get; set; } = 0;
    public List<uint> EncodedAttributeValueIndexToCornerMap { get; set; } = [];
    public List<int> VertexToEncodedAttributeValueIndexMap { get; set; } = [];

    public MeshAttributeIndicesEncodingData(int numVertices)
    {
        EncodedAttributeValueIndexToCornerMap = new List<uint>(numVertices);
        VertexToEncodedAttributeValueIndexMap.Resize(numVertices, 0);
    }
}
