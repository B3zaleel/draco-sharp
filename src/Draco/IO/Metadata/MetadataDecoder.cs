namespace Draco.IO.Metadata;

internal static class MetadataDecoder
{
    public static DracoMetadata Decode(DecoderBuffer decoderBuffer)
    {
        var attributesMetadataCount = (uint)decoderBuffer.DecodeVarIntUnsigned();
        var attributesMetadata = new MetadataElement[attributesMetadataCount];
        for (uint i = 0; i < attributesMetadataCount; ++i)
        {
            var id = (uint)decoderBuffer.DecodeVarIntUnsigned();
            attributesMetadata[i] = DecodeMetadataElement(decoderBuffer);
            attributesMetadata[i].Id = id;
        }
        var fileMetadata = DecodeMetadataElement(decoderBuffer);

        return new DracoMetadata
        {
            Attributes = attributesMetadata.ToList(),
            File = fileMetadata
        };
    }

    private static MetadataElement DecodeMetadataElement(DecoderBuffer decoderBuffer)
    {
        var numEntries = (uint)decoderBuffer.DecodeVarIntUnsigned();
        var metadata = new MetadataElement
        {
            Keys = new sbyte[numEntries][],
            Values = new sbyte[numEntries][]
        };
        for (uint i = 0; i < numEntries; ++i)
        {
            var keySize = decoderBuffer.ReadByte();
            var key = decoderBuffer.ReadSBytes(keySize);
            metadata.Keys[i] = key;
            var valueSize = decoderBuffer.ReadByte();
            var value = decoderBuffer.ReadSBytes(valueSize);
            metadata.Values[i] = value;
        }
        var numSubMetadata = (uint)decoderBuffer.DecodeVarIntUnsigned();
        for (uint i = 0; i < numSubMetadata; ++i)
        {
            var keySize = decoderBuffer.ReadByte();
            metadata.SubMetadataKeys[i] = decoderBuffer.ReadSBytes(keySize);
            metadata.SubMetadata![i] = DecodeMetadataElement(decoderBuffer);
        }
        return metadata;
    }
}
