namespace Draco.IO.Entropy;

internal interface ISymbolEncoder
{
    uint NumSymbols { get; }
    bool NeedsReverseEncoding { get; }

    void Create(EncoderBuffer encoderBuffer, int maxBitLength, ulong[] frequencies, int numSymbols);
    void StartEncoding(EncoderBuffer encoderBuffer);
    void EncodeSymbol(uint symbol);
    void EndEncoding(EncoderBuffer encoderBuffer);
}
