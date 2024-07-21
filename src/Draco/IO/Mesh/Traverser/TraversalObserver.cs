namespace Draco.IO.Mesh.Traverser;

internal abstract class TraversalObserver
{
    public abstract void OnNewFaceVisited(uint face);
    public abstract void OnNewVertexVisited(uint vertex, uint corner);
}
