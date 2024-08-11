using Draco.IO.BitCoders;
using Draco.IO.Extensions;

namespace Draco.IO.Mesh;

internal class MeshEdgeBreakerTraversalPredictiveDecoder : MeshEdgeBreakerTraversalDecoder
{
    private uint _numVertices;
    private readonly List<uint> _vertexValences = [];
    private RAnsBitDecoder? _predictionDecoder;
    private int _lastSymbol = -1;
    private int _predictedSymbol = -1;

    protected override void Traversal_SetNumEncodedVertices(uint numVertices)
    {
        _numVertices = numVertices;
    }

    protected override void Traversal_Start(DecoderBuffer decoderBuffer)
    {
        base.Traversal_Start(decoderBuffer);
        int numSplitSymbols = decoderBuffer.ReadInt32();
        Assertions.ThrowIf(numSplitSymbols < 0 || numSplitSymbols >= _numVertices);
        _vertexValences.Resize((int)_numVertices, 0U);
        _predictionDecoder = new RAnsBitDecoder();
        _predictionDecoder.StartDecoding(decoderBuffer);
    }

    /// <summary>
    /// Returns the next edge breaker symbol that was reached during the traversal.
    /// </summary>
    /// <param name="decoderBuffer"></param>
    /// <returns></returns>
    protected override uint DecodeSymbol(DecoderBuffer decoderBuffer)
    {
        if (_predictedSymbol != -1)
        {
            if (_predictionDecoder!.DecodeNextBit() != 0)
            {
                _lastSymbol = _predictedSymbol;
                return (uint)_predictedSymbol;
            }
        }
        _lastSymbol = (int)base.DecodeSymbol(decoderBuffer);
        return (uint)_lastSymbol;
    }

    protected override void NewActiveCornerReached(uint corner)
    {
        uint next = CornerTable!.Next(corner);
        uint prev = CornerTable.Previous(corner);
        switch (_lastSymbol)
        {
            case Constants.EdgeBreakerTopologyBitPattern.C:
            case Constants.EdgeBreakerTopologyBitPattern.S:
                _vertexValences[(int)CornerTable.Vertex(next)] += 1;
                _vertexValences[(int)CornerTable.Vertex(prev)] += 1;
                break;
            case Constants.EdgeBreakerTopologyBitPattern.R:
                _vertexValences[(int)CornerTable.Vertex(corner)] += 1;
                _vertexValences[(int)CornerTable.Vertex(next)] += 1;
                _vertexValences[(int)CornerTable.Vertex(prev)] += 2;
                break;
            case Constants.EdgeBreakerTopologyBitPattern.L:
                _vertexValences[(int)CornerTable.Vertex(corner)] += 1;
                _vertexValences[(int)CornerTable.Vertex(next)] += 2;
                _vertexValences[(int)CornerTable.Vertex(prev)] += 1;
                break;
            case Constants.EdgeBreakerTopologyBitPattern.E:
                _vertexValences[(int)CornerTable.Vertex(corner)] += 2;
                _vertexValences[(int)CornerTable.Vertex(next)] += 2;
                _vertexValences[(int)CornerTable.Vertex(prev)] += 2;
                break;
            default:
                break;
        }
        if (_lastSymbol == Constants.EdgeBreakerTopologyBitPattern.C || _lastSymbol == Constants.EdgeBreakerTopologyBitPattern.R)
        {
            var pivot = (int)CornerTable.Vertex(CornerTable.Next(corner));
            if (_vertexValences[pivot] < Constants.NumUniqueValences)
            {
                _predictedSymbol = Constants.EdgeBreakerTopologyBitPattern.R;
            }
            else
            {
                _predictedSymbol = Constants.EdgeBreakerTopologyBitPattern.C;
            }
        }
        else
        {
            _predictedSymbol = -1;
        }
    }

    protected override void MergeVertices(uint dest, uint source)
    {
        _vertexValences[(int)dest] += _vertexValences[(int)source];
    }
}
