using Draco.IO.Extensions;

namespace Draco.IO.Mesh.Traverser;

internal class MaxPredictionDegreeTraverser(CornerTable cornerTable, TraversalObserver traversalObserver) : Traverser(cornerTable, traversalObserver)
{
    private const int _kMaxPriority = 3;
    private readonly List<uint>[] _traversalStacks = new List<uint>[_kMaxPriority];
    private int _bestPriority;
    private readonly List<uint> _predictionDegrees = [];

    public override void Start()
    {
        base.Start();

        for (int i = 0; i < _kMaxPriority; ++i)
        {
            _traversalStacks[i] = new();
        }
    }

    public override void TraverseFromCorner(uint cornerId)
    {
        if (_predictionDegrees.Count == 0)
        {
            return;
        }
        _traversalStacks[0].Add(cornerId);
        _bestPriority = 0;
        var nextVertex = CornerTable.Vertex(CornerTable.Next(cornerId));
        var prevVertex = CornerTable.Vertex(CornerTable.Previous(cornerId));

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
        var tipVertex = CornerTable.Vertex(cornerId);

        if (!IsVertexVisited(tipVertex))
        {
            MarkVertexVisited(tipVertex);
            TraversalObserver.OnNewVertexVisited(tipVertex, cornerId);
        }
        cornerId = PopNextCornerToTraverse();

        while (cornerId != Constants.kInvalidCornerIndex)
        {
            var faceId = cornerId / 3;
        
            if (IsFaceVisited(faceId, true))
            {
                cornerId = PopNextCornerToTraverse();
                continue;
            }
            while (true)
            {
                faceId = cornerId / 3;
                MarkFaceVisited(faceId);
                TraversalObserver.OnNewFaceVisited(faceId);
                var vertexId = CornerTable.Vertex(cornerId);
                if (!IsVertexVisited(vertexId))
                {
                    MarkVertexVisited(vertexId);
                    TraversalObserver.OnNewVertexVisited(vertexId, cornerId);
                }
                var rightCornerId = CornerTable.GetRightCorner(cornerId);
                var leftCornerId = CornerTable.GetLeftCorner(cornerId);
                var rightFaceId = rightCornerId == Constants.kInvalidCornerIndex ? Constants.kInvalidFaceIndex : rightCornerId / 3;
                var leftFaceId = leftCornerId == Constants.kInvalidCornerIndex ? Constants.kInvalidFaceIndex : leftCornerId / 3;
                var isRightFaceVisited = IsFaceVisited(rightFaceId, true);
                var isLeftFaceVisited = IsFaceVisited(leftFaceId, true);

                if (!isLeftFaceVisited)
                {
                    var priority = ComputePriority(leftCornerId);

                    if (isRightFaceVisited && priority <= _bestPriority)
                    {
                        cornerId = leftCornerId;
                        continue;
                    }
                    else
                    {
                        AddCornerToTraversalStack(leftCornerId, priority);
                    }
                }
                if (!isRightFaceVisited)
                {
                    var priority = ComputePriority(rightCornerId);

                    if (priority <= _bestPriority)
                    {
                        cornerId = rightCornerId;
                        continue;
                    }
                    else
                    {
                        AddCornerToTraversalStack(rightCornerId, priority);
                    }
                }
                break;
            }
            cornerId = PopNextCornerToTraverse();
        }
    }

    private uint PopNextCornerToTraverse()
    {
        for (int i = _bestPriority; i < _kMaxPriority; ++i)
        {
            if (_traversalStacks[i].Count > 0)
            {
                var cornerId = _traversalStacks[i].Last();
                _traversalStacks[i].PopBack();
                _bestPriority = i;
                return cornerId;
            }
        }
        return Constants.kInvalidCornerIndex;
    }

    private void AddCornerToTraversalStack(uint cornerId, int priority)
    {
        _traversalStacks[priority].Add(cornerId);

        if (priority < _bestPriority)
        {
            _bestPriority = priority;
        }
    }

    private int ComputePriority(uint cornerId)
    {
        var vertexTip = CornerTable.Vertex(cornerId);
        int priority = 0;
        if (!IsVertexVisited(vertexTip))
        {
            var degree = ++_predictionDegrees[(int)vertexTip];
            priority = degree > 1 ? 1 : 2;
        }
        if (priority >= _kMaxPriority)
        {
            priority = _kMaxPriority - 1;
        }
        return priority;
    }
}
