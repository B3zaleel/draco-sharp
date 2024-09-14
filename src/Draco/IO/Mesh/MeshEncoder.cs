using Draco.IO.Attributes;

namespace Draco.IO.Mesh;

/// <summary>
/// Represents a class that constructs a 3D mesh data to input data that can be decoded by a <see cref="MeshDecoder"/>.
/// </summary>
internal abstract class MeshEncoder(Config config, Mesh mesh) : ConnectivityEncoder(config)
{
    public Mesh Mesh { get; protected set; } = mesh;
    public override PointCloud.PointCloud? PointCloud { get => Mesh; }
    public override int GeometryType { get => Constants.EncodingType.TriangularMesh; }
    public virtual CornerTable? CornerTable { get; protected set; }
    public int EncodedFacesCount { get; protected set; }

    public virtual MeshAttributeCornerTable? GetAttributeCornerTable(int attId)
    {
        return null;
    }

    public virtual MeshAttributeIndicesEncodingData? GetAttributeEncodingData(int attId)
    {
        return null;
    }

    public abstract void ComputeNumberOfEncodedFaces();
}
