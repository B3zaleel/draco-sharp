using Draco.IO.Attributes;

namespace Draco.IO.Mesh;

internal class DecoderAttributeData
{
    public int DecoderId { get; set; } = -1;
    public MeshAttributeCornerTable? ConnectivityData { get; set; }
    public bool IsConnectivityUsed { get; set; } = true;
    public MeshAttributeIndicesEncodingData? EncodingData { get; set; }
    public List<int> AttributeSeamCorners { get; set; } = [];
}
