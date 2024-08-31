using Draco.IO.Entropy;
using Draco.IO.Extensions;

namespace Draco.IO.Mesh;

internal class MeshEdgeBreakerTraversalValenceEncoder(Config config) : MeshEdgeBreakerTraversalEncoder(config)
{
    private readonly List<uint> _cornerToVertexMap = [];
    private readonly List<int> _vertexValences = [];
    private int _previousSymbol = -1;
    private uint _lastCorner = Constants.kInvalidCornerIndex;
    private byte _minValence = Constants.MinValence;
    private byte _maxValence = Constants.MaxValence;
    private readonly List<List<uint>> _contextSymbols = [];
    public int NumSymbols { get; private set; }

    protected override void Traversal_Init()
    {
        base.Traversal_Init();
        _minValence = Constants.MinValence;
        _maxValence = Constants.MaxValence;
        _vertexValences.Resize(CornerTable!.NewVerticesCount, 0);
        for (uint i = 0; i < _vertexValences.Count; ++i)
        {
            _vertexValences[(int)i] = CornerTable.VertexValence(i);
        }
        _cornerToVertexMap.Resize(CornerTable.CornersCount, Constants.kInvalidVertexIndex);
        for (uint i = 0; i < CornerTable.CornersCount; ++i)
        {
            _cornerToVertexMap[(int)i] = CornerTable.Vertex(i);
        }
        _contextSymbols.Resize(_maxValence - _minValence + 1, () => []);
    }

    protected override void Traversal_Done()
    {
        EncodeStartFaces();
        EncodeAttributeSeams();

        for (int i = 0; i < _contextSymbols.Count; ++i)
        {
            TraversalBuffer.EncodeVarIntUnsigned((uint)_contextSymbols[i].Count);

            if (_contextSymbols[i].Count > 0)
            {
                SymbolEncoding.EncodeSymbols(TraversalBuffer, null, _contextSymbols[i], 1);
            }
        }
    }

    protected override void EncodeSymbol(uint symbol)
    {
        ++NumSymbols;
        var next = CornerTable!.Next(_lastCorner);
        var previous = CornerTable.Previous(_lastCorner);
        var activeValence = _vertexValences[(int)_cornerToVertexMap[(int)next]];
        switch (symbol)
        {
            case Constants.EdgeBreakerTopologyBitPattern.C:
            case Constants.EdgeBreakerTopologyBitPattern.S:
                {
                    _vertexValences[(int)_cornerToVertexMap[(int)next]] -= 1;
                    _vertexValences[(int)_cornerToVertexMap[(int)previous]] -= 1;

                    if (symbol == Constants.EdgeBreakerTopologyBitPattern.S)
                    {
                        int numLeftFaces = 0;
                        var actC = CornerTable.Opposite(previous);

                        while (actC != Constants.kInvalidCornerIndex)
                        {
                            if (IsFaceEncoded(CornerTable.Face(actC)))
                            {
                                break;
                            }
                            ++numLeftFaces;
                            actC = CornerTable.Opposite(CornerTable.Next(actC));
                        }
                        _vertexValences[(int)_cornerToVertexMap[(int)_lastCorner]] = numLeftFaces + 1;
                        int newVertexId = _vertexValences.Count;
                        int numRightFaces = 0;
                        actC = CornerTable.Opposite(next);
                        while (actC != Constants.kInvalidCornerIndex)
                        {
                            if (IsFaceEncoded(CornerTable.Face(actC)))
                            {
                                break;
                            }
                            ++numRightFaces;
                            _cornerToVertexMap[(int)CornerTable.Next(actC)] = (uint)newVertexId;
                            actC = CornerTable.Opposite(CornerTable.Previous(actC));
                        }
                        _vertexValences.Add(numRightFaces + 1);
                    }
                    break;
                }
            case Constants.EdgeBreakerTopologyBitPattern.R:
                {
                    _vertexValences[(int)_cornerToVertexMap[(int)_lastCorner]] -= 1;
                    _vertexValences[(int)_cornerToVertexMap[(int)next]] -= 1;
                    _vertexValences[(int)_cornerToVertexMap[(int)previous]] -= 2;
                    break;
                }
            case Constants.EdgeBreakerTopologyBitPattern.L:
                {
                    _vertexValences[(int)_cornerToVertexMap[(int)_lastCorner]] -= 1;
                    _vertexValences[(int)_cornerToVertexMap[(int)next]] -= 2;
                    _vertexValences[(int)_cornerToVertexMap[(int)previous]] -= 1;
                    break;
                }
            case Constants.EdgeBreakerTopologyBitPattern.E:
                {
                    _vertexValences[(int)_cornerToVertexMap[(int)_lastCorner]] -= 2;
                    _vertexValences[(int)_cornerToVertexMap[(int)next]] -= 2;
                    _vertexValences[(int)_cornerToVertexMap[(int)previous]] -= 2;
                    break;
                }
            default:
                break;
        }
        if (_previousSymbol != -1)
        {
            int clampedValence;

            if (activeValence < _minValence)
            {
                clampedValence = _minValence;
            }
            else if (activeValence > _maxValence)
            {
                clampedValence = _maxValence;
            }
            else
            {
                clampedValence = activeValence;
            }
            _contextSymbols[clampedValence - _minValence].Add(Constants.EdgeBreakerTopologyToSymbolId[_previousSymbol]);
        }
        _previousSymbol = (int)symbol;
    }

    protected override void NewActiveCornerReached(uint corner)
    {
        _lastCorner = corner;
    }
}
