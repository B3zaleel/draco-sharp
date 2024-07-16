using System.Collections;

namespace Draco.IO.Mesh;

internal class VertexCornersIterator : IEnumerable<uint>
{
    private readonly CornerTable _cornerTable;
    private readonly uint _startCorner;
    private bool _leftTraversal = true;

    public uint Current { get; set; }

    public VertexCornersIterator(CornerTable cornerTable, uint id, bool isVertexId)
    {
        _cornerTable = cornerTable;
        _startCorner = isVertexId ? cornerTable.LeftMostCorner(id) : id;
        Current = _startCorner;
        _leftTraversal = true;
    }

    public bool MoveNext()
    {
        if (Current == Constants.kInvalidCornerIndex)
        {
            return false;
        }
        if (_leftTraversal)
        {
            Current = _cornerTable.SwingLeft(Current);
            if (Current == Constants.kInvalidCornerIndex)
            {
                Current = _cornerTable.SwingRight(_startCorner);
                _leftTraversal = false;
            }
            else if (Current == _startCorner)
            {
                Current = Constants.kInvalidCornerIndex;
            }
        }
        else
        {
            Current = _cornerTable.SwingRight(Current);
        }
        return true;
    }

    public void Reset()
    {
        Current = _startCorner;
        _leftTraversal = true;
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
