using Draco.IO.Extensions;

namespace Draco.IO.Mesh;

internal class ValenceCache(CornerTable cornerTable)
{
    private readonly CornerTable _table = cornerTable;
    private readonly List<sbyte> _vertexValenceCache8Bit = [];
    private readonly List<int> _vertexValenceCache32Bit = [];

    public void CacheValencesInaccurate()
    {
        if (_vertexValenceCache8Bit.Count == 0)
        {
            for (uint v = 0; v < _table.VerticesCount; v++)
            {
                _vertexValenceCache8Bit.Add((sbyte)_table.VertexValence(v));
            }
        }
    }

    public void CacheValences()
    {
        if (_vertexValenceCache32Bit.Count == 0)
        {
            for (uint v = 0; v < _table.VerticesCount; v++)
            {
                _vertexValenceCache32Bit.Add(_table.VertexValence(v));
            }
        }
    }

    public int CornerValenceFromCache(uint c)
    {
        return c == Constants.kInvalidCornerIndex ? -1 : ConfidentVertexValenceFromCache(_table.Vertex(c));
    }

    public int ConfidentVertexValenceFromCache(uint v)
    {
        Assertions.ThrowIfNot(v < _table.VerticesCount && _vertexValenceCache32Bit.Count == _table.VerticesCount);
        return _vertexValenceCache32Bit[(int)v];
    }

    public int ConfidentCornerValenceFromCacheInaccurate(uint c)
    {
        Assertions.ThrowIfNot(c >= 0);
        return ConfidentVertexValenceFromCacheInaccurate(_table.ConfidentVertex(c));
    }

    public int ConfidentCornerValenceFromCache(uint c)
    {
        Assertions.ThrowIfNot(c >= 0);
        return ConfidentVertexValenceFromCache(_table.ConfidentVertex(c));
    }

    public int VertexValenceFromCacheInaccurate(uint v)
    {
        Assertions.ThrowIfNot(_vertexValenceCache8Bit.Count == _table.VerticesCount);
        if (v == Constants.kInvalidVertexIndex || v >= _table.VerticesCount)
        {
            return -1;
        }
        return ConfidentVertexValenceFromCacheInaccurate(v);
    }

    public int ConfidentVertexValenceFromCacheInaccurate(uint v)
    {
        Assertions.ThrowIfNot(v < _table.VerticesCount && _vertexValenceCache8Bit.Count == _table.VerticesCount);
        return _vertexValenceCache8Bit[(int)v];
    }

    public int VertexValenceFromCache(uint v)
    {
        Assertions.ThrowIfNot(_vertexValenceCache32Bit.Count == _table.VerticesCount);
        if (v == Constants.kInvalidVertexIndex || v >= _table.VerticesCount)
        {
            return -1;
        }
        return ConfidentVertexValenceFromCache(v);
    }

    public void ClearValenceCacheInaccurate()
    {
        _vertexValenceCache8Bit.Clear();
    }

    public void ClearValenceCache()
    {
        _vertexValenceCache32Bit.Clear();
    }

    public bool IsCacheEmpty()
    {
        return _vertexValenceCache8Bit.Count == 0 && _vertexValenceCache32Bit.Count == 0;
    }
}
