using Draco.IO.Attributes;
using Draco.IO.Extensions;

namespace Draco.IO.Mesh;

public class Mesh : PointCloud.PointCloud
{
    /// <summary>
    /// Mesh specific per-attribute data.
    /// </summary>
    private readonly List<MeshAttributeData> _attributeData = [];
    private readonly List<int[]> _faces = [];

    public int FacesCount { get => _faces.Count; }

    public void AddFace(int[] face)
    {
        _faces.Add(face);
    }

    public void SetFace(uint faceId, int[] face)
    {
        if (faceId >= _faces.Count)
        {
            _faces.AddRange(new int[(int)faceId - _faces.Count + 1][]);
        }
        _faces[(int)faceId] = face;
    }

    public void SetNumFaces(int numFaces)
    {
        _faces.AddRange(new int[numFaces][]);
    }

    public int[] GetFace(uint faceId)
    {
        Assertions.ThrowIfNot(faceId >= 0 && faceId < _faces.Count);
        return _faces[(int)faceId];
    }

    public override void SetAttribute(int attId, PointAttribute pointAttribute)
    {
        base.SetAttribute(attId, pointAttribute);
        if (_attributeData.Count <= attId)
        {
            _attributeData.Resize(attId + 1, () => new());
        }
    }

    public MeshAttributeElementType GetAttributeElementType(int attId)
    {
        return _attributeData[attId].ElementType;
    }

    public void SetAttributeElementType(int attId, MeshAttributeElementType elementType)
    {
        _attributeData[attId].ElementType = elementType;
    }

    public uint CornerToPointId(uint cornerId)
    {
        int ci = (int)cornerId;
        if (ci < 0 || cornerId == Constants.kInvalidCornerIndex)
        {
            return Constants.kInvalidCornerIndex;
        }
        return (uint)_faces[ci / 3][ci % 3];
    }
}
