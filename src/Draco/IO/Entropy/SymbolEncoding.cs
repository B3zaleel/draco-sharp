using Draco.IO.Enums;
using Draco.IO.Extensions;

namespace Draco.IO.Entropy;

internal static class SymbolEncoding
{
    public static void EncodeSymbols(EncoderBuffer encoderBuffer, Config? config, List<uint> values, int numComponents)
    {
        var valuesAsArray = values.ToArray();
        var (bitLengths, maxValue) = ComputeBitLengths(valuesAsArray, numComponents);
        var taggedSchemeTotalBits = ApproximateTaggedSchemeBits(bitLengths, numComponents);
        var (rawSchemeTotalBits, numUniqueSymbols) = ApproximateRawSchemeBits(valuesAsArray, maxValue);
        var maxValueBitLength = BitUtilities.MostSignificantBit(Math.Max(1, maxValue)) + 1;
        byte method;
        if (config != null && config.IsOptionSet(ConfigOptionName.SymbolEncodingMethod))
        {
            method = config.GetOption<byte>(ConfigOptionName.SymbolEncodingMethod, 0);
        }
        else
        {
            if (taggedSchemeTotalBits < rawSchemeTotalBits || maxValueBitLength < Constants.MaxRawEncodingBitLength)
            {
                method = Constants.SymbolCoding.Tagged;
            }
            else
            {
                method = Constants.SymbolCoding.Raw;
            }
        }
        encoderBuffer.WriteByte(method);
        if (method == Constants.SymbolCoding.Tagged)
        {
            EncodeTaggedSymbols(encoderBuffer, valuesAsArray, numComponents, bitLengths);
        }
        else
        {
            EncodeRawSymbols(encoderBuffer, config, valuesAsArray, maxValue, numUniqueSymbols);
        }
    }

    private static (uint[] BitLengths, uint MaxValue) ComputeBitLengths(uint[] symbols, int numComponents)
    {
        var bitLengths = new uint[symbols.Length / numComponents];
        int bitLengthIndex = 0;
        uint maxValue = 0;

        for (int i = 0; i < symbols.Length; i += numComponents)
        {
            uint maxComponentValue = symbols[i];
            for (int j = 1; j < numComponents; ++j)
            {
                if (maxComponentValue < symbols[i + j])
                {
                    maxComponentValue = symbols[i + j];
                }
            }
            int valueMSBPosition = 0;
            if (maxComponentValue > 0)
            {
                valueMSBPosition = BitUtilities.MostSignificantBit(maxComponentValue);
            }
            if (maxComponentValue > maxValue)
            {
                maxValue = maxComponentValue;
            }
            bitLengths[bitLengthIndex++] = (uint)(valueMSBPosition + 1);
        }
        return (bitLengths, maxValue);
    }

    private static long ApproximateTaggedSchemeBits(uint[] bitLengths, int numComponents)
    {
        uint totalBitLength = 0;

        for (int i = 0; i < bitLengths.Length; i++)
        {
            totalBitLength += bitLengths[i];
        }
        var (tagBits, numUniqueSymbols) = ShannonEntropy.ComputeShannonEntropy(bitLengths, 32);
        var tagTableBits = RAnsSymbolCoding.ApproximateRAnsFrequencyTableBits(numUniqueSymbols, numUniqueSymbols);
        return tagBits + tagTableBits + totalBitLength * numComponents;
    }

    private static (long TotalBits, int NumUniqueSymbols) ApproximateRawSchemeBits(uint[] symbols, uint maxValue)
    {
        var (dataBits, numUniqueSymbols) = ShannonEntropy.ComputeShannonEntropy(symbols, (int)maxValue);
        var tableBits = RAnsSymbolCoding.ApproximateRAnsFrequencyTableBits((int)maxValue, numUniqueSymbols);
        return (tableBits + dataBits, numUniqueSymbols);
    }

    private static void EncodeTaggedSymbols(EncoderBuffer encoderBuffer, uint[] symbols, int numComponents, uint[] bitLengths)
    {
        var frequencies = new ulong[Constants.MaxTagSymbolBitLength];
        for (int i = 0; i < bitLengths.Length; ++i)
        {
            ++frequencies[bitLengths[i]];
        }
        var valueBuffer = new EncoderBuffer(new BinaryWriter(new MemoryStream()));
        ulong valueBits = (ulong)(Constants.MaxTagSymbolBitLength * symbols.Length);
        ISymbolEncoder tagEncoder = new RAnsSymbolEncoder();
        tagEncoder.Create(encoderBuffer, 5, frequencies, Constants.MaxTagSymbolBitLength);
        tagEncoder.StartEncoding(encoderBuffer);
        valueBuffer.StartBitEncoding(false, valueBits);

        if (tagEncoder.NeedsReverseEncoding)
        {
            for (int i = symbols.Length - numComponents; i >= 0; i -= numComponents)
            {
                var bitLength = bitLengths[i / numComponents];
                tagEncoder.EncodeSymbol(bitLength);
                int j = symbols.Length - numComponents - i;
                byte valueBitLength = (byte)bitLengths[j / numComponents];

                for (int c = 0; c < numComponents; ++c)
                {
                    valueBuffer.EncodeLeastSignificantBits32(valueBitLength, symbols[j + c]);
                }
            }
        }
        else
        {
            for (int i = 0; i < symbols.Length; i += numComponents)
            {
                var bitLength = (byte)bitLengths[i / numComponents];
                tagEncoder.EncodeSymbol(bitLength);

                for (int j = 0; j < numComponents; ++j)
                {
                    valueBuffer.EncodeLeastSignificantBits32(bitLength, symbols[i + j]);
                }
            }
        }
        tagEncoder.EndEncoding(encoderBuffer);
        valueBuffer.EndBitEncoding();
        encoderBuffer.WriteBytes(valueBuffer.Data);
    }

    private static void EncodeRawSymbols(EncoderBuffer encoderBuffer, Config? config, uint[] symbols, uint maxEntryValue, int numUniqueSymbols)
    {
        int symbolBits = numUniqueSymbols > 0 ? BitUtilities.MostSignificantBit((uint)numUniqueSymbols) : 0;
        int uniqueSymbolsBitLength = symbolBits + 1;
        Assertions.ThrowIf(uniqueSymbolsBitLength > Constants.MaxRawEncodingBitLength, "Currently, we don't support encoding of more than 2^18 unique symbols.");
        int compressionLevel = Constants.DefaultSymbolCodingCompressionLevel;

        if (config != null && config.IsOptionSet(ConfigOptionName.SymbolEncodingCompressionLevel))
        {
            compressionLevel = config.GetOption(ConfigOptionName.SymbolEncodingCompressionLevel, Constants.DefaultSymbolCodingCompressionLevel);
        }
        if (compressionLevel < 4)
        {
            uniqueSymbolsBitLength -= 2;
        }
        else if (compressionLevel < 6)
        {
            uniqueSymbolsBitLength -= 1;
        }
        else if (compressionLevel > 9)
        {
            uniqueSymbolsBitLength += 2;
        }
        else if (compressionLevel > 7)
        {
            uniqueSymbolsBitLength += 1;
        }
        uniqueSymbolsBitLength = Math.Min(Math.Max(1, uniqueSymbolsBitLength), Constants.MaxRawEncodingBitLength);
        encoderBuffer.WriteByte((byte)uniqueSymbolsBitLength);
        var frequencies = new ulong[maxEntryValue + 1];
        for (int i = 0; i < symbols.Length; ++i)
        {
            ++frequencies[symbols[i]];
        }
        Assertions.ThrowIf(uniqueSymbolsBitLength > 18, "Currently, we don't support encoding of more than 2^18 unique symbols.");
        ISymbolEncoder encoder = new RAnsSymbolEncoder();
        encoder.Create(encoderBuffer, uniqueSymbolsBitLength, frequencies, frequencies.Length);
        encoder.StartEncoding(encoderBuffer);
        if (encoder.NeedsReverseEncoding)
        {
            for (int i = symbols.Length - 1; i >= 0; --i)
            {
                encoder.EncodeSymbol(symbols[i]);
            }
        }
        else
        {
            for (int i = 0; i < symbols.Length; ++i)
            {
                encoder.EncodeSymbol(symbols[i]);
            }
        }
        encoder.EndEncoding(encoderBuffer);
    }
}
