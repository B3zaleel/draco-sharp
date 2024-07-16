using System.Collections;

namespace Draco.IO.Mesh;

internal class VertexRingIterator : IEnumerable<uint>
{
    private readonly CornerTable _cornerTable;
    private readonly uint _startCorner;
    private bool _leftTraversal = true;

    public uint Current { get; set; }

    public VertexRingIterator(CornerTable cornerTable, uint vertexId)
    {
        _cornerTable = cornerTable;
        _startCorner = cornerTable.LeftMostCorner(vertexId);
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
                Current = _startCorner;
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
