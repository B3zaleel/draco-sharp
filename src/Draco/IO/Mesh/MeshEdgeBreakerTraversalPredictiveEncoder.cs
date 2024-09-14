using Draco.IO.BitCoders;
using Draco.IO.Extensions;

namespace Draco.IO.Mesh;

internal class MeshEdgeBreakerTraversalPredictiveEncoder(Config config, Mesh mesh) : MeshEdgeBreakerTraversalEncoder(config, mesh)
{
    private readonly List<int> _vertexValences = [];
    private readonly List<bool> _predictions = [];
    private int _previousSymbol = -1;
    private uint _numSplitSymbols = 0;
    private uint _lastCorner = Constants.kInvalidCornerIndex;
    public int NumSymbols { get; private set; }

    protected override void Traversal_Init()
    {
        base.Traversal_Init();
        _vertexValences.Resize(CornerTable!.NewVerticesCount, 0);
        for (uint i = 0; i < _vertexValences.Count; ++i)
        {
            _vertexValences[(int)i] = CornerTable.VertexValence(i);
        }
    }

    protected override void Traversal_Done()
    {
        if (_previousSymbol != -1)
        {
            base.EncodeSymbol((uint)_previousSymbol);
        }
        base.Traversal_Done();
        TraversalBuffer.Write(_numSplitSymbols);
        var predictionEncoder = new RAnsBitEncoder();

        for (int i = _predictions.Count - 1; i >= 0; --i)
        {
            predictionEncoder.EncodeBit(_predictions[i]);
        }
        predictionEncoder.EndEncoding(TraversalBuffer);
    }

    protected override void EncodeSymbol(uint symbol)
    {
        ++NumSymbols;
        int predictedSymbol = -1;
        var next = CornerTable!.Next(_lastCorner);
        var previous = CornerTable.Previous(_lastCorner);

        switch (symbol)
        {
            case Constants.EdgeBreakerTopologyBitPattern.C:
            case Constants.EdgeBreakerTopologyBitPattern.S:
                {
                    if (symbol == Constants.EdgeBreakerTopologyBitPattern.C)
                    {
                        predictedSymbol = ComputePredictedSymbol(CornerTable.Vertex(next));
                    }
                    _vertexValences[(int)CornerTable.Vertex(next)] -= 1;
                    _vertexValences[(int)CornerTable.Vertex(previous)] -= 1;

                    if (symbol == Constants.EdgeBreakerTopologyBitPattern.S)
                    {
                        _vertexValences[(int)CornerTable.Vertex(_lastCorner)] = -1;
                        ++_numSplitSymbols;
                    }
                    break;
                }
            case Constants.EdgeBreakerTopologyBitPattern.R:
                {
                    predictedSymbol = ComputePredictedSymbol(CornerTable.Vertex(next));
                    _vertexValences[(int)CornerTable.Vertex(_lastCorner)] -= 1;
                    _vertexValences[(int)CornerTable.Vertex(next)] -= 1;
                    _vertexValences[(int)CornerTable.Vertex(previous)] -= 2;
                    break;
                }
            case Constants.EdgeBreakerTopologyBitPattern.L:
                {
                    _vertexValences[(int)CornerTable.Vertex(_lastCorner)] -= 1;
                    _vertexValences[(int)CornerTable.Vertex(next)] -= 2;
                    _vertexValences[(int)CornerTable.Vertex(previous)] -= 1;
                    break;
                }
            case Constants.EdgeBreakerTopologyBitPattern.E:
                {
                    _vertexValences[(int)CornerTable.Vertex(_lastCorner)] -= 2;
                    _vertexValences[(int)CornerTable.Vertex(next)] -= 2;
                    _vertexValences[(int)CornerTable.Vertex(previous)] -= 2;
                    break;
                }
            default:
                break;
        }
        var storePreviousSymbol = true;
        if (_previousSymbol != -1)
        {
            if (predictedSymbol == _previousSymbol)
            {
                _predictions.Add(true);
                storePreviousSymbol = false;
            }
            else if (_previousSymbol != -1)
            {
                _predictions.Add(false);
            }
        }
        if (storePreviousSymbol && _previousSymbol != -1)
        {
            base.EncodeSymbol((uint)_previousSymbol);
        }
        _previousSymbol = (int)symbol;
    }

    private int ComputePredictedSymbol(uint pivot)
    {
        int valence = _vertexValences[(int)pivot];

        if (valence < 0)
        {
            return Constants.EdgeBreakerTopologyBitPattern.Invalid;
        }
        return valence < 6 ? Constants.EdgeBreakerTopologyBitPattern.R : Constants.EdgeBreakerTopologyBitPattern.C;
    }

    protected override void NewCornerReached(uint corner)
    {
        _lastCorner = corner;
    }
}
