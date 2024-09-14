using Draco.IO.BitCoders;

namespace Draco.IO.Mesh;

internal class MeshEdgeBreakerTraversalEncoder(Config config, Mesh mesh) : MeshEdgeBreakerEncoder(config, mesh)
{
    private uint _numAttributeData = 0;
    private RAnsBitEncoder _startFaceEncoder = new();
    private List<uint> _symbols = [];
    private RAnsBitEncoder[] _attributeConnectivityEncoders = [];
    public override int NumEncodedSymbols { get => _symbols.Count; }

    protected override void Traversal_Init()
    {
        _numAttributeData = 0;
        _startFaceEncoder = new();
        _symbols = [];
        _attributeConnectivityEncoders = [];
    }

    protected override void Traversal_SetNumAttributeData(uint numData)
    {
        _numAttributeData = numData;
    }

    protected override void Traversal_Start()
    {
        _startFaceEncoder.StartEncoding(TraversalBuffer);
        if (_numAttributeData > 0)
        {
            _attributeConnectivityEncoders = new RAnsBitEncoder[_numAttributeData];
            for (uint i = 0; i < _numAttributeData; ++i)
            {
                _attributeConnectivityEncoders[i] = new RAnsBitEncoder();
                _attributeConnectivityEncoders[i].StartEncoding(TraversalBuffer);
            }
        }
    }

    protected override void Traversal_Done()
    {
        EncodeTraversalSymbols();
        EncodeStartFaces();
        EncodeAttributeSeams();
    }

    protected void EncodeTraversalSymbols()
    {
        TraversalBuffer.StartBitEncoding(true, (ulong)Mesh.FacesCount * 3);
        for (int i = _symbols.Count - 1; i >= 0; --i)
        {
            TraversalBuffer.EncodeLeastSignificantBits32(Constants.EdgeBreakerTopologyBitPatternLength[_symbols[i]], _symbols[i]);
        }
        TraversalBuffer.EndBitEncoding();
    }

    protected void EncodeStartFaces()
    {
        _startFaceEncoder.EndEncoding(TraversalBuffer);
    }

    protected void EncodeAttributeSeams()
    {
        if (_attributeConnectivityEncoders.Length > 0)
        {
            for (uint i = 0; i < _numAttributeData; ++i)
            {
                _attributeConnectivityEncoders[i].EndEncoding(TraversalBuffer);
            }
        }
    }

    protected override void EncodeSymbol(uint symbol)
    {
        _symbols.Add(symbol);
    }

    protected override void EncodeAttributeSeam(int attributeId, bool isSeam)
    {
        _attributeConnectivityEncoders[attributeId].EncodeBit(isSeam);
    }

    protected override void EncodeStartFaceConfiguration(EncoderBuffer encoderBuffer, bool interior)
    {
        _startFaceEncoder.EncodeBit(interior);
    }
}
