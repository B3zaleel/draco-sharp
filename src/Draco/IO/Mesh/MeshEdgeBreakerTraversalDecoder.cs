using Draco.IO.BitCoders;

namespace Draco.IO.Mesh;

internal class MeshEdgeBreakerTraversalDecoder : MeshEdgeBreakerDecoder
{
    private uint _numAttributeData = 0;
    private RAnsBitDecoder _startFaceDecoder = new();
    private DecoderBuffer? _symbolDecoderBuffer;
    private DecoderBuffer? _startFaceDecoderBuffer;
    private RAnsBitDecoder[] _attributeConnectivityDecoders = [];

    protected override void Traversal_Init(DecoderBuffer decoderBuffer)
    {
        _startFaceDecoder = new RAnsBitDecoder();
    }

    protected override void Traversal_SetNumEncodedVertices(uint numVertices)
    {
    }

    protected override void Traversal_SetNumAttributeData(uint numData)
    {
        _numAttributeData = numData;
    }

    protected override void Traversal_Start(DecoderBuffer decoderBuffer)
    {
        DecodeTraversalSymbols(decoderBuffer);
        DecodeStartFaces(decoderBuffer);
        DecodeAttributeSeams(decoderBuffer);
    }

    protected void DecodeTraversalSymbols(DecoderBuffer decoderBuffer)
    {
        decoderBuffer.StartBitDecoding(true, out ulong traversalSize);
        _symbolDecoderBuffer = new DecoderBuffer(decoderBuffer.ReadBytes((int)traversalSize), decoderBuffer.BitStream_Version);
    }

    protected void DecodeStartFaces(DecoderBuffer decoderBuffer)
    {
        if (decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 2))
        {
            decoderBuffer.StartBitDecoding(true, out ulong traversalSize);
            _startFaceDecoderBuffer = new DecoderBuffer(decoderBuffer.ReadBytes((int)traversalSize), decoderBuffer.BitStream_Version);
        }
        else
        {
            _startFaceDecoder.StartDecoding(decoderBuffer);
        }
    }

    protected void DecodeAttributeSeams(DecoderBuffer decoderBuffer)
    {
        if (_numAttributeData > 0)
        {
            _attributeConnectivityDecoders = new RAnsBitDecoder[_numAttributeData];
            for (uint i = 0; i < _numAttributeData; ++i)
            {
                _attributeConnectivityDecoders[i] = new RAnsBitDecoder();
                _attributeConnectivityDecoders[i].StartDecoding(decoderBuffer);
            }
        }
    }

    protected override void Traversal_Done(DecoderBuffer decoderBuffer)
    {
        decoderBuffer.EndBitDecoding();
        if (decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 2))
        {
            _startFaceDecoderBuffer!.EndBitDecoding();
        }
        else
        {
            _startFaceDecoder.EndDecoding(decoderBuffer);
        }
    }

    protected override uint DecodeAttributeSeam(int attribute)
    {
        return _attributeConnectivityDecoders[attribute].DecodeNextBit();
    }

    /// <summary>
    /// Returns the next edge breaker symbol that was reached during the traversal.
    /// </summary>
    /// <param name="decoderBuffer"></param>
    /// <returns></returns>
    protected override uint DecodeSymbol(DecoderBuffer decoderBuffer)
    {
        uint symbol = _symbolDecoderBuffer!.DecodeLeastSignificantBits32(1);
        if (symbol == Constants.EdgeBreakerTopologyBitPattern.C)
        {
            return symbol;
        }
        uint symbolSuffix = _symbolDecoderBuffer.DecodeLeastSignificantBits32(2);
        symbol |= symbolSuffix << 1;
        return symbol;
    }

    protected override bool DecodeStartFaceConfiguration(DecoderBuffer decoderBuffer)
    {
        uint faceConfiguration = decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 2)
            ? _startFaceDecoderBuffer!.DecodeLeastSignificantBits32(1)
            : _startFaceDecoder.DecodeNextBit();
        return (faceConfiguration & 1) == 1;
    }
}
