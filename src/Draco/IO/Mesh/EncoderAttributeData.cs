using Draco.IO.Attributes;
using Draco.IO.Enums;

namespace Draco.IO.Mesh;

internal class EncoderAttributeData
{
    public int AttributeIndex { get; set; } = -1;
    public MeshAttributeCornerTable? ConnectivityData { get; set; }
    public bool IsConnectivityUsed { get; set; } = true;
    public MeshAttributeIndicesEncodingData? EncodingData { get; set; }
    public MeshTraversalMethod TraversalMethod { get; set; }
}
