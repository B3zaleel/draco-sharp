using Draco.IO.Extensions;

namespace Draco.IO.Mesh;

internal class CornerTable
{
    // Each three consecutive corners represent one face.
    protected readonly List<uint> _cornerToVertexMap = [];
    private readonly List<uint> _oppositeCorners = [];
    private readonly List<uint> _vertexCorners = [];
    private readonly List<uint> _nonManifoldVertexParents = [];

    public int VerticesCount { get => _vertexCorners.Count; }
    public int CornersCount { get => _cornerToVertexMap.Count; }
    public int FacesCount { get => _cornerToVertexMap.Count / 3; }
    public int NewVerticesCount { get => VerticesCount - OriginalVerticesCount; }
    public int OriginalVerticesCount { get; private set; } = 0;
    public int DegeneratedFacesCount { get; private set; } = 0;
    public int IsolatedVerticesCount { get; private set; } = 0;
    public ValenceCache ValenceCache { get; private set; }

    public CornerTable()
    {
        ValenceCache = new(this);
    }

    public void Reset(int numFaces)
    {
        Reset(numFaces, numFaces * 3);
    }

    public void Reset(int numFaces, int numVertices)
    {
        Assertions.ThrowIf(numFaces < 0 || numVertices < 0);
        _cornerToVertexMap.Fill(numFaces * 3, Constants.kInvalidVertexIndex);
        _oppositeCorners.Fill(numFaces * 3, Constants.kInvalidCornerIndex);
        ValenceCache.ClearValenceCache();
        ValenceCache.ClearValenceCacheInaccurate();
    }

    public virtual uint Opposite(uint corner)
    {
        return corner == Constants.kInvalidCornerIndex ? corner : _oppositeCorners[(int)corner];
    }

    public virtual uint Next(uint corner)
    {
        return corner == Constants.kInvalidCornerIndex ? corner : LocalIndex(++corner) != 0 ? corner : corner - 3;
    }

    public virtual uint Previous(uint corner)
    {
        return corner == Constants.kInvalidCornerIndex ? corner : LocalIndex(corner) != 0 ? corner - 1 : corner + 2;
    }

    public virtual uint Vertex(uint corner)
    {
        return corner == Constants.kInvalidCornerIndex || corner >= CornersCount ? corner : ConfidentVertex(corner);
    }

    public virtual uint ConfidentVertex(uint corner)
    {
        Assertions.ThrowIfNot(corner >= 0 && corner < CornersCount);
        return _cornerToVertexMap[(int)corner];
    }

    public virtual uint Face(uint corner)
    {
        return corner == Constants.kInvalidCornerIndex ? corner : corner / 3;
    }

    public virtual uint FirstCorner(uint face)
    {
        return face == Constants.kInvalidFaceIndex ? face : face * 3;
    }

    public virtual uint[] AllCorners(uint face)
    {
        var ci = face * 3;
        return [ci, ci + 1, ci + 2];
    }

    private uint LocalIndex(uint corner)
    {
        return corner % 3;
    }

    public uint[] FaceData(uint face)
    {
        var firstCorner = FirstCorner(face);
        var faceData = new uint[3];
        for (byte i = 0; i < 3; ++i)
        {
            faceData[i] = _cornerToVertexMap[(int)firstCorner + i];
        }
        return faceData;
    }

    public void SetFaceData(uint face, uint[] data)
    {
        Assertions.ThrowIfNot(ValenceCache.IsCacheEmpty());
        var firstCorner = FirstCorner(face);
        for (int i = 0; i < 3; ++i)
        {
            _cornerToVertexMap[(int)firstCorner + i] = data[i];
        }
    }

    public virtual uint LeftMostCorner(uint v)
    {
        return _vertexCorners[(int)v];
    }

    public virtual uint VertexParent(uint vertex)
    {
        return vertex < OriginalVerticesCount ? vertex : _nonManifoldVertexParents[(int)vertex - OriginalVerticesCount];
    }

    public virtual int VertexValence(uint v)
    {
        if (v == Constants.kInvalidVertexIndex)
        {
            return (int)v;
        }
        return ConfidentVertexValence(v);
    }

    public virtual int ConfidentVertexValence(uint v)
    {
        Assertions.ThrowIfNot(v >= 0 || v < VerticesCount);
        int valence = 0;
        foreach (var corner in new VertexCornersIterator(this, v, true))
        {
            ++valence;
        }
        return valence;
    }

    public virtual int CornerValence(uint c)
    {
        return c == Constants.kInvalidCornerIndex ? (int)c : ConfidentCornerValence(c);
    }

    public virtual int ConfidentCornerValence(uint c)
    {
        Assertions.ThrowIfNot(c < CornersCount);
        return ConfidentVertexValence(ConfidentVertex(c));
    }

    public virtual bool IsOnBoundary(uint vert)
    {
        var corner = LeftMostCorner(vert);
        return SwingLeft(corner) == Constants.kInvalidCornerIndex;
    }

    public virtual uint SwingRight(uint corner)
    {
        return Previous(Opposite(Previous(corner)));
    }

    public virtual uint SwingLeft(uint corner)
    {
        return Next(Opposite(Next(corner)));
    }

    public virtual uint GetLeftCorner(uint cornerId)
    {
        return cornerId == Constants.kInvalidCornerIndex ? Constants.kInvalidCornerIndex : Opposite(Previous(cornerId));
    }

    public virtual uint GetRightCorner(uint cornerId)
    {
        return cornerId == Constants.kInvalidCornerIndex ? Constants.kInvalidCornerIndex : Opposite(Next(cornerId));
    }

    public void SetOppositeCorner(uint cornerId, uint oppCornerId)
    {
        Assertions.ThrowIfNot(ValenceCache.IsCacheEmpty());
        _oppositeCorners[(int)cornerId] = oppCornerId;
    }

    public void SetOppositeCorners(uint corner0, uint corner1)
    {
        Assertions.ThrowIfNot(ValenceCache.IsCacheEmpty());
        if (corner0 != Constants.kInvalidCornerIndex)
        {
            SetOppositeCorner(corner0, corner1);
        }
        if (corner1 != Constants.kInvalidCornerIndex)
        {
            SetOppositeCorner(corner1, corner0);
        }
    }

    public void MapCornerToVertex(uint cornerId, uint vertId)
    {
        Assertions.ThrowIfNot(ValenceCache.IsCacheEmpty());
        _cornerToVertexMap[(int)cornerId] = vertId;
    }

    public uint AddNewVertex()
    {
        Assertions.ThrowIfNot(ValenceCache.IsCacheEmpty());
        _vertexCorners.Add(Constants.kInvalidVertexIndex);
        return (uint)_vertexCorners.Count - 1;
    }

    public int AddNewFace(uint[] vertices)
    {
        var newFaceIndex = FacesCount;
        for (byte i = 0; i < 3; i++)
        {
            _cornerToVertexMap.Add(vertices[i]);
            SetLeftMostCorner(vertices[i], (uint)_cornerToVertexMap.Count - 1);
        }
        _oppositeCorners.Resize(_cornerToVertexMap.Count, Constants.kInvalidCornerIndex);
        return newFaceIndex;
    }

    public void SetLeftMostCorner(uint vert, uint corner)
    {
        Assertions.ThrowIfNot(ValenceCache.IsCacheEmpty());
        if (vert != Constants.kInvalidVertexIndex)
        {
            _vertexCorners[(int)vert] = corner;
        }
    }

    public void UpdateVertexToCornerMap(uint vert)
    {
        Assertions.ThrowIfNot(ValenceCache.IsCacheEmpty());
        var firstC = _vertexCorners[(int)vert];
        if (firstC == Constants.kInvalidCornerIndex)
        {
            return;
        }
        var actC = SwingLeft(firstC);
        var c = firstC;

        while (actC != Constants.kInvalidAttributeValueIndex && actC != firstC)
        {
            c = actC;
            actC = SwingLeft(actC);
        }
        if (actC != firstC)
        {
            _vertexCorners[(int)vert] = c;
        }
    }

    public void SetNumVertices(int numVertices)
    {
        Assertions.ThrowIfNot(ValenceCache.IsCacheEmpty());
        _vertexCorners.Resize(numVertices, Constants.kInvalidCornerIndex);
    }

    public void MakeVertexIsolated(uint vert)
    {
        Assertions.ThrowIfNot(ValenceCache.IsCacheEmpty());
        _vertexCorners[(int)vert] = uint.MaxValue;
    }

    public bool IsVertexIsolated(uint v)
    {
        return LeftMostCorner(v) == Constants.kInvalidCornerIndex;
    }

    public void MakeFaceInvalid(uint face)
    {
        Assertions.ThrowIfNot(ValenceCache.IsCacheEmpty());
        if (face != uint.MaxValue)
        {
            var firstCorner = FirstCorner(face);
            for (byte i = 0; i < 3; ++i)
            {
                _cornerToVertexMap[(int)firstCorner + i] = uint.MaxValue;
            }
        }
    }

    public void ComputeOppositeCorners(ref int? numVertices)
    {
        Assertions.ThrowIfNot(ValenceCache.IsCacheEmpty());
        Assertions.ThrowIf(numVertices == null);
        _oppositeCorners.Resize(CornersCount, Constants.kInvalidCornerIndex);
        List<int> numCornersOnVertices = [];
        numCornersOnVertices.Fill(CornersCount, 0);

        for (uint c = 0; c < CornersCount; ++c)
        {
            var v1 = Vertex(c);
            if (v1 >= numCornersOnVertices.Count)
            {
                numCornersOnVertices.Resize((int)v1 + 1, 0);
            }
            numCornersOnVertices[(int)v1]++;
        }
        List<VertexEdgePair> vertexEdges = [];
        vertexEdges.Fill(CornersCount, () => new());
        List<int>? vertexOffset = [];
        vertexOffset.Fill(numCornersOnVertices.Count, 0);
        int offset = 0;

        for (int i = 0; i < numCornersOnVertices.Count; ++i)
        {
            vertexOffset[i] = offset;
            offset += numCornersOnVertices[i];
        }
        for (uint c = 0; c < CornersCount; ++c)
        {
            var tipV = Vertex(c);
            var sourceV = Vertex(Next(c));
            var sinkV = Vertex(Previous(c));
            var faceIndex = Face(c);
            if (c == FirstCorner(faceIndex))
            {
                var v0 = Vertex(c);
                if (v0 == sourceV || v0 == sinkV || sourceV == sinkV)
                {
                    ++DegeneratedFacesCount;
                    c += 2;
                    continue;
                }
            }
            var oppositeC = Constants.kInvalidCornerIndex;
            int numCornersOnVert = numCornersOnVertices[(int)sinkV];
            offset = vertexOffset[(int)sinkV];

            for (int i = 0; i < numCornersOnVert; ++i)
            {
                var otherV = vertexEdges[offset].SinkVert;
                if (otherV == Constants.kInvalidVertexIndex)
                {
                    break;
                }
                if (otherV == sourceV)
                {
                    if (tipV == Vertex(vertexEdges[offset].EdgeCorner))
                    {
                        continue;
                    }
                    oppositeC = vertexEdges[offset].EdgeCorner;
                    for (int j = i + 1; j < numCornersOnVert; ++j, ++offset)
                    {
                        vertexEdges[offset] = vertexEdges[offset + 1];
                        if (vertexEdges[offset].SinkVert == Constants.kInvalidVertexIndex)
                        {
                            break;
                        }
                    }
                    vertexEdges[offset].SinkVert = Constants.kInvalidVertexIndex;
                    break;
                }
            }
            if (oppositeC == Constants.kInvalidCornerIndex)
            {
                int numCornersOnSourceVert = numCornersOnVertices[(int)sourceV];
                offset = vertexOffset[(int)sourceV];

                for (int i = 0; i < numCornersOnSourceVert; ++i, ++offset)
                {
                    if (vertexEdges[offset].SinkVert == Constants.kInvalidVertexIndex)
                    {
                        vertexEdges[offset].SinkVert = sinkV;
                        vertexEdges[offset].EdgeCorner = c;
                        break;
                    }
                }
            }
            else
            {
                _oppositeCorners[(int)c] = oppositeC;
                _oppositeCorners[(int)oppositeC] = c;
            }
        }
        numVertices = numCornersOnVertices.Count;
    }

    public void BreakNonManifoldEdges()
    {
        List<bool> visitedCorners = [];
        visitedCorners.Fill(CornersCount, false);
        List<(uint, uint)> sinkVertices = [];
        bool meshConnectivityUpdated;

        do
        {
            meshConnectivityUpdated = false;

            for (uint c = 0; c < CornersCount; ++c)
            {
                if (visitedCorners[(int)c])
                {
                    continue;
                }
                sinkVertices.Clear();
                var firstC = c;
                var currentC = c;
                var nextC = SwingLeft(currentC);

                while (nextC != firstC && nextC != Constants.kInvalidCornerIndex && !visitedCorners[(int)nextC])
                {
                    currentC = nextC;
                    nextC = SwingLeft(currentC);
                }
                firstC = currentC;

                do
                {
                    visitedCorners[(int)currentC] = true;
                    var sinkC = Next(currentC);
                    var sinkV = _cornerToVertexMap[(int)sinkC];
                    var edgeCorner = Previous(currentC);
                    var vertexConnectivityUpdated = false;

                    foreach (var attachedSinkVertex in sinkVertices)
                    {
                        if (attachedSinkVertex.Item1 == sinkV)
                        {
                            var otherEdgeCorner = attachedSinkVertex.Item2;
                            var oppEdgeCorner = Opposite(edgeCorner);

                            if (oppEdgeCorner == otherEdgeCorner)
                            {
                                continue;
                            }
                            var oppOtherEdgeCorner = Opposite(otherEdgeCorner);
                            if (oppEdgeCorner != Constants.kInvalidCornerIndex)
                            {
                                SetOppositeCorner(oppEdgeCorner, Constants.kInvalidCornerIndex);
                            }
                            if (oppOtherEdgeCorner != Constants.kInvalidCornerIndex)
                            {
                                SetOppositeCorner(oppOtherEdgeCorner, Constants.kInvalidCornerIndex);
                            }
                            SetOppositeCorner(edgeCorner, Constants.kInvalidCornerIndex);
                            SetOppositeCorner(otherEdgeCorner, Constants.kInvalidCornerIndex);
                            vertexConnectivityUpdated = true;
                            break;
                        }
                    }
                    if (vertexConnectivityUpdated)
                    {
                        meshConnectivityUpdated = true;
                        break;
                    }
                    sinkVertices.Add((_cornerToVertexMap[(int)Previous(currentC)], sinkC));
                    currentC = SwingRight(currentC);
                } while (currentC != firstC && firstC != Constants.kInvalidCornerIndex);
            }
        } while (meshConnectivityUpdated);
    }

    public void ComputeVertexCorners(int numVertices)
    {
        Assertions.ThrowIfNot(ValenceCache.IsCacheEmpty());
        OriginalVerticesCount = numVertices;
        _vertexCorners.Resize(numVertices, Constants.kInvalidCornerIndex);
        List<bool> visitedVertices = [];
        List<bool> visitedCorners = [];
        visitedVertices.Fill(numVertices, false);
        visitedCorners.Fill(CornersCount, false);

        for (uint f = 0; f < FacesCount; ++f)
        {
            var firstFaceCorner = FirstCorner(f);
            if (IsDegenerated(f))
            {
                continue;
            }
            for (byte k = 0; k < 3; ++k)
            {
                var c = firstFaceCorner + k;
                if (visitedCorners[(int)c])
                {
                    continue;
                }
                var v = _cornerToVertexMap[(int)c];
                var isNonManifoldVertex = false;
                if (visitedVertices[(int)v])
                {
                    _vertexCorners.Add(Constants.kInvalidCornerIndex);
                    _nonManifoldVertexParents.Add(v);
                    visitedVertices.Add(false);
                    v = (uint)numVertices++;
                    isNonManifoldVertex = true;
                }
                visitedVertices[(int)v] = true;
                var actC = c;
                while (actC != Constants.kInvalidCornerIndex)
                {
                    visitedCorners[(int)actC] = true;
                    _vertexCorners[(int)v] = actC;

                    if (isNonManifoldVertex)
                    {
                        _cornerToVertexMap[(int)actC] = v;
                    }
                    actC = SwingLeft(actC);

                    if (actC == c)
                    {
                        break;
                    }
                }
                if (actC == Constants.kInvalidCornerIndex)
                {
                    actC = SwingRight(c);
                    while (actC != Constants.kInvalidCornerIndex)
                    {
                        visitedCorners[(int)actC] = true;
                        if (isNonManifoldVertex)
                        {
                            _cornerToVertexMap[(int)actC] = v;
                        }
                        actC = SwingRight(actC);
                    }
                }
            }
        }
        IsolatedVerticesCount = 0;

        foreach (var visited in visitedVertices)
        {
            if (!visited)
            {
                ++IsolatedVerticesCount;
            }
        }
    }

    public virtual bool IsDegenerated(uint face)
    {
        if (face == Constants.kInvalidFaceIndex)
        {
            return true;
        }
        var firstFaceCorner = FirstCorner(face);
        var v0 = Vertex(firstFaceCorner);
        var v1 = Vertex(Next(firstFaceCorner));
        var v2 = Vertex(Previous(firstFaceCorner));
        return v0 == v1 || v0 == v2 || v1 == v2;
    }

    public void UpdateFaceToVertexMap(uint vertex)
    {
        Assertions.ThrowIfNot(ValenceCache.IsCacheEmpty());
        foreach (var cornerId in new VertexCornersIterator(this, vertex, true))
        {
            _cornerToVertexMap[(int)cornerId] = vertex;
        }
    }

    private class VertexEdgePair
    {
        public uint SinkVert { get; set; } = 0;
        public uint EdgeCorner { get; set; } = 0;
    }
}
