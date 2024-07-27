using Draco.IO.Attributes;

namespace Draco.IO.Mesh;

/// <summary>
/// represents a class that reconstructs a 3D mesh from input data that was encoded by a <see cref="IMeshEncoder"/>.
/// </summary>
internal abstract class MeshDecoder : ConnectivityDecoder
{
    public Mesh Mesh { get; protected set; } = new();
    public new PointCloud.PointCloud? PointCloud { get => Mesh; }
    public new int GeometryType { get => Constants.EncodingType.TriangularMesh; }
    public virtual CornerTable? CornerTable { get => null; }

    public virtual MeshAttributeCornerTable? GetAttributeCornerTable(int attId)
    {
        return null;
    }

    public virtual MeshAttributeIndicesEncodingData? GetAttributeEncodingData(int attId)
    {
        return null;
    }
}
