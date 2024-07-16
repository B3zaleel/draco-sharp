using System.Collections;

namespace Draco.IO.Mesh;

internal class FaceAdjacencyIterator : IEnumerable<uint>
{
    private readonly CornerTable _cornerTable;
    private readonly uint _startCorner;
    private uint _corner;

    public uint Current { get => _cornerTable.Face(_cornerTable.Opposite(_corner)); }

    public FaceAdjacencyIterator(CornerTable cornerTable, uint faceId)
    {
        _cornerTable = cornerTable;
        _startCorner = cornerTable.FirstCorner(faceId);
        _corner = _startCorner;
        if (cornerTable.Opposite(_corner) == Constants.kInvalidCornerIndex)
        {
            FindNextNeighbor();
        }
    }

    public bool MoveNext()
    {
        if (Current == Constants.kInvalidCornerIndex)
        {
            return false;
        }
        FindNextNeighbor();
        return true;
    }

    public void FindNextNeighbor()
    {
        while (_corner != Constants.kInvalidVertexIndex)
        {
            _corner = _cornerTable.Next(_corner);
            if (_corner == _startCorner)
            {
                _corner = Constants.kInvalidCornerIndex;
                return;
            }
            if (_cornerTable.Opposite(_corner) != Constants.kInvalidCornerIndex)
            {
                return;
            }
        }
    }

    public void Reset()
    {
        _corner = _startCorner;
        if (_cornerTable.Opposite(_corner) == Constants.kInvalidCornerIndex)
        {
            FindNextNeighbor();
        }
    }

    public IEnumerator<uint> GetEnumerator()
    {
        while (MoveNext())
        {
            yield return Current;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        while (MoveNext())
        {
            yield return Current;
        }
    }
}
