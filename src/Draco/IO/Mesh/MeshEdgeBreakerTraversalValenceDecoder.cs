using Draco.IO.Entropy;
using Draco.IO.Extensions;

namespace Draco.IO.Mesh;

internal class MeshEdgeBreakerTraversalValenceDecoder : MeshEdgeBreakerTraversalDecoder
{
    private int _numVertices;
    private readonly List<uint> _vertexValences = [];
    private int _lastSymbol = -1;
    private int _activeContext = -1;
    private byte _minValence = Constants.MinValence;
    private byte _maxValence = Constants.MaxValence;
    private readonly List<List<uint>> _contextSymbols = [];
    private readonly List<int> _contextCounters = [];

    protected override void Traversal_SetNumEncodedVertices(uint numVertices)
    {
        _numVertices = (int)numVertices;
    }

    protected override void Traversal_Start(DecoderBuffer decoderBuffer)
    {
        if (decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 2))
        {
            DecodeTraversalSymbols(decoderBuffer);
        }
        DecodeStartFaces(decoderBuffer);
        DecodeAttributeSeams(decoderBuffer);
        if (decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 2))
        {
            uint numSplitSymbols = decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 0)
                ? decoderBuffer.ReadUInt32()
                : (uint)decoderBuffer.DecodeVarIntUnsigned();
            Assertions.ThrowIf(numSplitSymbols >= _numVertices);
            var mode = decoderBuffer.ReadSByte();
            if (mode == Constants.EdgeBreakerValenceCodingMode.EdgeBreakerValenceMode_2_7)
            {
                _minValence = Constants.MinValence;
                _maxValence = Constants.MaxValence;
            }
            else
            {
                Assertions.Throw($"Unsupported mode {mode}.");
            }
        }
        else
        {
            _minValence = Constants.MinValence;
            _maxValence = Constants.MaxValence;
        }
        _vertexValences.Fill(_numVertices, 0U);
        int numUniqueValences = _maxValence - _minValence + 1;
        _contextSymbols.Fill(numUniqueValences, () => []);
        _contextCounters.Fill(numUniqueValences, 0);

        for (int i = 0; i < numUniqueValences; ++i)
        {
            uint numSymbols = (uint)decoderBuffer.DecodeVarIntUnsigned();
            Assertions.ThrowIf(numSymbols > _cornerTable!.FacesCount);
            if (numSymbols > 0)
            {
                SymbolDecoding.DecodeSymbols(decoderBuffer, numSymbols, 1, out uint[] contextSymbolsData);
                _contextSymbols[i] = contextSymbolsData.ToList();
                _contextCounters[i] = (int)numSymbols;
            }
        }
    }

    /// <summary>
    /// Returns the next edge breaker symbol that was reached during the traversal.
    /// </summary>
    /// <param name="decoderBuffer"></param>
    /// <returns></returns>
    protected override uint DecodeSymbol(DecoderBuffer decoderBuffer)
    {
        if (_activeContext != -1)
        {
            int contextCounter = --_contextCounters[_activeContext];
            if (contextCounter < 0)
            {
                return Constants.EdgeBreakerTopologyBitPattern.Invalid;
            }
            var symbolId = _contextSymbols[_activeContext][contextCounter];
            if (symbolId > 4)
            {
                return Constants.EdgeBreakerTopologyBitPattern.Invalid;
            }
            _lastSymbol = Constants.EdgeBreakerSymbolToTopologyId[symbolId];
        }
        else
        {
            _lastSymbol = decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 2)
                ? (int)base.DecodeSymbol(decoderBuffer)
                : Constants.EdgeBreakerTopologyBitPattern.E;
        }
        return (uint)_lastSymbol;
    }

    protected override void NewActiveCornerReached(uint corner)
    {
        uint next = _cornerTable!.Next(corner);
        uint prev = _cornerTable.Previous(corner);
        switch (_lastSymbol)
        {
            case Constants.EdgeBreakerTopologyBitPattern.C:
            case Constants.EdgeBreakerTopologyBitPattern.S:
                _vertexValences[(int)_cornerTable.Vertex(next)] += 1;
                _vertexValences[(int)_cornerTable.Vertex(prev)] += 1;
                break;
            case Constants.EdgeBreakerTopologyBitPattern.R:
                _vertexValences[(int)_cornerTable.Vertex(corner)] += 1;
                _vertexValences[(int)_cornerTable.Vertex(next)] += 1;
                _vertexValences[(int)_cornerTable.Vertex(prev)] += 2;
                break;
            case Constants.EdgeBreakerTopologyBitPattern.L:
                _vertexValences[(int)_cornerTable.Vertex(corner)] += 1;
                _vertexValences[(int)_cornerTable.Vertex(next)] += 2;
                _vertexValences[(int)_cornerTable.Vertex(prev)] += 1;
                break;
            case Constants.EdgeBreakerTopologyBitPattern.E:
                _vertexValences[(int)_cornerTable.Vertex(corner)] += 2;
                _vertexValences[(int)_cornerTable.Vertex(next)] += 2;
                _vertexValences[(int)_cornerTable.Vertex(prev)] += 2;
                break;
            default:
                break;
        }
        int activeValence = (int)_vertexValences[(int)_cornerTable.Vertex(next)];
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
        _activeContext = clampedValence - _minValence;
    }

    protected override void MergeVertices(uint dest, uint source)
    {
        _vertexValences[(int)dest] += _vertexValences[(int)source];
    }
}
