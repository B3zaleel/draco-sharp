using Draco.IO.Extensions;

namespace Draco.IO.Mesh.Traverser;

internal abstract class Traverser
{
    private readonly List<bool> _isFaceVisited = [];
    private readonly List<bool> _isVertexVisited = [];
    public CornerTable CornerTable { get; set; }
    public TraversalObserver TraversalObserver { get; set; }

    public Traverser(CornerTable cornerTable, TraversalObserver traversalObserver)
    {
        _isFaceVisited.Fill(cornerTable.FacesCount, false);
        _isVertexVisited.Fill(cornerTable.VerticesCount, false);
        CornerTable = cornerTable;
        TraversalObserver = traversalObserver;
    }

    public bool IsFaceVisited(uint id, bool isFaceId)
    {
        if (isFaceId)
        {
            return id == Constants.kInvalidFaceIndex || _isFaceVisited[(int)id];
        }
        else
        {
            return id == Constants.kInvalidCornerIndex || _isFaceVisited[(int)(id / 3)];
        }
    }

    public void MarkFaceVisited(uint faceId)
    {
        _isFaceVisited[(int)faceId] = true;
    }

    public bool IsVertexVisited(uint vertexId)
    {
        return _isVertexVisited[(int)vertexId];
    }

    public void MarkVertexVisited(uint vertexId)
    {
        _isVertexVisited[(int)vertexId] = true;
    }

    public virtual void Start() { }
    public virtual void End() { }
    public abstract void TraverseFromCorner(uint cornerId);
}
