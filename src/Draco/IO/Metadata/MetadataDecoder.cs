namespace Draco.IO.Metadata;

internal class MetadataDecoder
{
    public MetadataElement[]? AttMetadata;
    public MetadataElement? FileMetadata;

    public void Decode(DecoderBuffer decoderBuffer)
    {
        var num_att_metadata = (uint)decoderBuffer.DecodeVarIntUnsigned();
        AttMetadata = new MetadataElement[num_att_metadata];
        for (uint i = 0; i < num_att_metadata; ++i)
        {
            var id = (uint)decoderBuffer.DecodeVarIntUnsigned();
            AttMetadata[i] = DecodeMetadataElement(decoderBuffer);
            AttMetadata[i].Id = id;
        }
        FileMetadata = DecodeMetadataElement(decoderBuffer);
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
