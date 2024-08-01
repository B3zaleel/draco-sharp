using Draco.IO.Attributes;
using Draco.IO.Extensions;

namespace Draco.IO.Mesh;

internal class MeshAttributeCornerTable : CornerTable
{
    private readonly ValenceCache? _valenceCache;
    private readonly List<bool> _isEdgeOnSeam = [];
    private readonly List<bool> _isVertexOnSeam = [];
    private readonly List<uint> _vertexToLeftMostCornerMap = [];
    private readonly List<uint> _vertexToAttributeEntryIdMap = [];

    public new int VerticesCount { get => _vertexToAttributeEntryIdMap.Count; }
    public new int CornersCount { get => CornerTable.CornersCount; }
    public new int FacesCount { get => CornerTable.FacesCount; }
    public bool NoInteriorSeams { get; private set; }
    public CornerTable CornerTable { get; private set; }

    public MeshAttributeCornerTable(CornerTable cornerTable)
    {
        CornerTable = cornerTable;
        _valenceCache = new(this);
        ValenceCache.ClearValenceCache();
        ValenceCache.ClearValenceCacheInaccurate();
        _isEdgeOnSeam.Fill(cornerTable.CornersCount, false);
        _isVertexOnSeam.Fill(cornerTable.VerticesCount, false);
        _cornerToVertexMap.Fill(cornerTable.CornersCount, Constants.kInvalidVertexIndex);
        NoInteriorSeams = true;
    }

    public void AddSeamEdge(uint corner)
    {
        Assertions.ThrowIfNot(ValenceCache.IsCacheEmpty());
        _isEdgeOnSeam[(int)corner] = true;
        _isVertexOnSeam[(int)CornerTable.Vertex(CornerTable.Next(corner))] = true;
        _isVertexOnSeam[(int)CornerTable.Vertex(CornerTable.Previous(corner))] = true;
        var oppCorner = CornerTable.Opposite(corner);

        if (oppCorner != Constants.kInvalidCornerIndex)
        {
            NoInteriorSeams = false;
            _isEdgeOnSeam[(int)oppCorner] = true;
            _isVertexOnSeam[(int)CornerTable.Vertex(CornerTable.Next(oppCorner))] = true;
            _isVertexOnSeam[(int)CornerTable.Vertex(CornerTable.Previous(oppCorner))] = true;
        }
    }

    public void RecomputeVertices(Mesh? mesh, PointAttribute? attribute)
    {
        Assertions.ThrowIfNot(ValenceCache.IsCacheEmpty());
        var initVertexToAttributeEntryMap = mesh != null && attribute != null;
        _vertexToAttributeEntryIdMap.Clear();
        _vertexToLeftMostCornerMap.Clear();
        int numNewVertices = 0;

        for (uint v = 0; v < VerticesCount; ++v)
        {
            var c = CornerTable.LeftMostCorner(v);
            if (c == Constants.kInvalidCornerIndex)
            {
                continue;
            }
            uint firstVertIndex = (uint)numNewVertices++;
            if (initVertexToAttributeEntryMap)
            {
                var pointId = mesh!.CornerToPointId(c);
                _vertexToAttributeEntryIdMap.Add(attribute!.MappedIndex(pointId));
            }
            else
            {
                _vertexToAttributeEntryIdMap.Add(firstVertIndex);
            }
            var firstC = c;
            uint actC;
            if (_isVertexOnSeam[(int)v])
            {
                actC = SwingLeft(firstC);
                while (actC != Constants.kInvalidCornerIndex)
                {
                    firstC = actC;
                    actC = SwingLeft(actC);
                    Assertions.ThrowIf(actC == c, "We reached the initial corner which shouldn't happen when we swing left from |c|.");
                }
            }
            _cornerToVertexMap[(int)firstC] = firstVertIndex;
            _vertexToLeftMostCornerMap.Add(firstC);
            actC = SwingRight(firstC);
            while (actC != Constants.kInvalidCornerIndex && actC != firstC)
            {
                if (IsCornerOppositeToSeamEdge(CornerTable.Next(actC)))
                {
                    firstVertIndex = (uint)numNewVertices++;
                    if (initVertexToAttributeEntryMap)
                    {
                        var pointId = mesh!.CornerToPointId(actC);
                        _vertexToAttributeEntryIdMap.Add(attribute!.MappedIndex(pointId));
                    }
                    else
                    {
                        _vertexToAttributeEntryIdMap.Add(firstVertIndex);
                    }
                    _vertexToLeftMostCornerMap.Add(firstC);
                }
                _cornerToVertexMap[(int)actC] = firstVertIndex;
                actC = SwingRight(actC);
            }
        }
    }

    public bool IsCornerOppositeToSeamEdge(uint corner)
    {
        return _isEdgeOnSeam[(int)corner];
    }

    public override uint Opposite(uint corner)
    {
        if (corner == Constants.kInvalidCornerIndex || IsCornerOppositeToSeamEdge(corner))
        {
            return Constants.kInvalidCornerIndex;
        }
        return CornerTable.Opposite(corner);
    }

    public override uint Next(uint corner)
    {
        return CornerTable.Next(corner);
    }

    public override uint Previous(uint corner)
    {
        return CornerTable.Previous(corner);
    }

    public bool IsCornerOnSeam(uint corner)
    {
        return _isVertexOnSeam[(int)CornerTable.Vertex(corner)];
    }

    public override uint GetLeftCorner(uint corner)
    {
        return Opposite(Previous(corner));
    }

    public override uint GetRightCorner(uint corner)
    {
        return Opposite(Next(corner));
    }

    public override uint SwingRight(uint corner)
    {
        return Previous(Opposite(Previous(corner)));
    }

    public override uint SwingLeft(uint corner)
    {
        return Next(Opposite(Next(corner)));
    }

    public override uint Vertex(uint corner)
    {
        Assertions.ThrowIfNot(corner < _cornerToVertexMap.Count);
        return ConfidentVertex(corner);
    }

    public override uint ConfidentVertex(uint corner)
    {
        return _cornerToVertexMap[(int)corner];
    }

    public override uint VertexParent(uint corner)
    {
        return _vertexToAttributeEntryIdMap[(int)corner];
    }

    public override uint LeftMostCorner(uint corner)
    {
        return _vertexToLeftMostCornerMap[(int)corner];
    }

    public override uint Face(uint corner)
    {
        return CornerTable.Face(corner);
    }

    public override uint FirstCorner(uint face)
    {
        return CornerTable.FirstCorner(face);
    }

    public override uint[] AllCorners(uint face)
    {
        return CornerTable.AllCorners(face);
    }

    public override bool IsOnBoundary(uint vert)
    {
        var corner = base.LeftMostCorner(vert);
        return corner == Constants.kInvalidCornerIndex || SwingLeft(corner) == Constants.kInvalidCornerIndex;
    }

    public override bool IsDegenerated(uint face)
    {
        return CornerTable.IsDegenerated(face);
    }

    public override int VertexValence(uint v)
    {
        return v == Constants.kInvalidVertexIndex ? -1 : ConfidentVertexValence(v);
    }

    public override int ConfidentVertexValence(uint v)
    {
        Assertions.ThrowIfNot(v < VerticesCount);
        int valence = 0;
        foreach (var vi in new VertexRingIterator(this, v))
        {
            ++valence;
        }
        return valence;
    }

    public override int CornerValence(uint c)
    {
        Assertions.ThrowIfNot(c < CornersCount);
        return c == Constants.kInvalidVertexIndex ? -1 : ConfidentCornerValence(c);
    }

    public override int ConfidentCornerValence(uint c)
    {
        Assertions.ThrowIfNot(c < CornersCount);
        return ConfidentVertexValence(Vertex(c));
    }
}
