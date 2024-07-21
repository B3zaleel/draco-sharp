using Draco.IO.Extensions;

namespace Draco.IO.Mesh.Traverser;

internal class DepthFirstTraverser(CornerTable cornerTable, TraversalObserver traversalObserver) : Traverser(cornerTable, traversalObserver)
{
    private readonly List<uint> _cornerTraversalStack = [];

    public override void TraverseFromCorner(uint cornerId)
    {
        if (IsFaceVisited(cornerId, false))
        {
            return;
        }
        _cornerTraversalStack.Clear();
        _cornerTraversalStack.Add(cornerId);
        var nextVertex = CornerTable.Vertex(CornerTable.Next(cornerId));
        var prevVertex = CornerTable.Vertex(CornerTable.Previous(cornerId));
        Assertions.ThrowIf(nextVertex == Constants.kInvalidVertexIndex || prevVertex == Constants.kInvalidVertexIndex);

        if (!IsVertexVisited(nextVertex))
        {
            MarkVertexVisited(nextVertex);
            TraversalObserver.OnNewVertexVisited(nextVertex, CornerTable.Next(cornerId));
        }
        if (!IsVertexVisited(prevVertex))
        {
            MarkVertexVisited(prevVertex);
            TraversalObserver.OnNewVertexVisited(prevVertex, CornerTable.Previous(cornerId));
        }
        while (_cornerTraversalStack.Count > 0)
        {
            cornerId = _cornerTraversalStack.Last();
            var faceId = cornerId / 3;

            if (cornerId == Constants.kInvalidCornerIndex || IsFaceVisited(faceId, true))
            {
                _cornerTraversalStack.PopBack();
                continue;
            }
            while (true)
            {
                MarkFaceVisited(faceId);
                TraversalObserver.OnNewFaceVisited(faceId);
                var vertexId = CornerTable.Vertex(cornerId);
                Assertions.ThrowIf(vertexId == Constants.kInvalidVertexIndex);

                if (!IsVertexVisited(vertexId))
                {
                    var onBoundary = CornerTable.IsOnBoundary(vertexId);
                    MarkVertexVisited(vertexId);
                    TraversalObserver.OnNewVertexVisited(vertexId, cornerId);

                    if (!onBoundary)
                    {
                        cornerId = CornerTable.GetRightCorner(cornerId);
                        faceId = cornerId / 3;
                        continue;
                    }
                }
                var rightCornedId = CornerTable.GetRightCorner(cornerId);
                var leftCornerId = CornerTable.GetLeftCorner(cornerId);
                var rightFaceId = rightCornedId == Constants.kInvalidCornerIndex
                    ? Constants.kInvalidFaceIndex
                    : rightCornedId / 3;
                var leftFaceId = leftCornerId == Constants.kInvalidCornerIndex
                    ? Constants.kInvalidFaceIndex
                    : leftCornerId / 3;

                if (IsFaceVisited(rightFaceId, true))
                {
                    if (IsFaceVisited(leftFaceId, true))
                    {
                        _cornerTraversalStack.PopBack();
                        break;
                    }
                    else
                    {
                        cornerId = leftCornerId;
                        faceId = leftFaceId;
                    }
                }
                else
                {
                    if (IsFaceVisited(leftFaceId, true))
                    {
                        cornerId = rightCornedId;
                        faceId = rightFaceId;
                    }
                    else
                    {
                        _cornerTraversalStack[_cornerTraversalStack.Count - 1] = leftCornerId;
                        _cornerTraversalStack.Add(rightCornedId);
                        break;
                    }
                }
            }
        }
    }
}
