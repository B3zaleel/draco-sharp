namespace Draco.IO.Entropy;

internal interface ISymbolDecoder
{
    uint NumSymbols { get; }

    void Create(DecoderBuffer decoderBuffer, int maxBitLength);
    void StartDecoding(DecoderBuffer decoderBuffer);
    uint DecodeSymbol();
    void EndDecoding();
}
