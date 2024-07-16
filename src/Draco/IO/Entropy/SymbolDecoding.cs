using Draco.IO.Extensions;

namespace Draco.IO.Entropy;

internal static class SymbolDecoding
{
    public static void DecodeSymbols(DecoderBuffer decoderBuffer, uint numValues, uint numComponents, out uint[] values)
    {
        if (numValues == 0)
        {
            values = [];
            return;
        }
        var scheme = decoderBuffer.ReadByte();

        if (scheme == Constants.SymbolCoding.Tagged)
        {
            DecodeTaggedSymbols(decoderBuffer, numValues, numComponents, out values);
        }
        else if (scheme == Constants.SymbolCoding.Raw)
        {
            DecodeRawSymbols(decoderBuffer, numValues, out values);
        }
        else
        {
            throw new InvalidDataException($"Unsupported Draco symbols scheme {scheme}.");
        }
    }

    private static void DecodeTaggedSymbols(DecoderBuffer decoderBuffer, uint numValues, uint numComponents, out uint[] values)
    {
        values = [];
        ISymbolDecoder tagDecoder = new RAnsSymbolDecoder();
        tagDecoder.Create(decoderBuffer, 5);
        tagDecoder.StartDecoding(decoderBuffer);
        Assertions.ThrowIf(numValues > 0 && tagDecoder.NumSymbols == 0, "Wrong number of symbols.");
        decoderBuffer.StartBitDecoding(false, out ulong _);
        int value_id = 0;
        for (uint i = 0; i < numValues; i += numComponents)
        {
            var bitLength = (byte)tagDecoder.DecodeSymbol();
            for (int j = 0; j < numComponents; ++j)
            {
                var value = decoderBuffer.DecodeLeastSignificantBits32(bitLength);
                values[value_id++] = value;
            }
        }
        tagDecoder.EndDecoding();
        decoderBuffer.EndBitDecoding();
    }

    private static void DecodeRawSymbols(DecoderBuffer decoderBuffer, uint numValues, out uint[] values)
    {
        values = new uint[numValues];
        var maxBitLength = decoderBuffer.ReadByte();
        Assertions.ThrowIf(maxBitLength < 1 || maxBitLength > 18);
        var decoder = new RAnsSymbolDecoder();
        decoder.Create(decoderBuffer, maxBitLength);
        Assertions.ThrowIf(numValues > 0 && decoder.NumSymbols == 0, "Wrong number of symbols.");
        decoder.StartDecoding(decoderBuffer);

        for (uint i = 0; i < numValues; i++)
        {
            values[i] = decoder.DecodeSymbol();
        }
        decoder.EndDecoding();
    }
}
