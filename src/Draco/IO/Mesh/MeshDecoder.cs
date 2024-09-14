using Draco.IO.Attributes;

namespace Draco.IO.Mesh;

/// <summary>
/// Represents a class that reconstructs a 3D mesh from input data that was encoded by a <see cref="MeshEncoder"/>.
/// </summary>
internal abstract class MeshDecoder : ConnectivityDecoder
{
    public Mesh Mesh { get; protected set; } = new();
    public override PointCloud.PointCloud? PointCloud { get => Mesh; }
    public override int GeometryType { get => Constants.EncodingType.TriangularMesh; }
    public virtual CornerTable? CornerTable { get; protected set; }

    public virtual MeshAttributeCornerTable? GetAttributeCornerTable(int attId)
    {
        return null;
    }

    public virtual MeshAttributeIndicesEncodingData? GetAttributeEncodingData(int attId)
    {
        return null;
    }
}
