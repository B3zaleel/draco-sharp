namespace Draco.IO.Metadata;

internal class MetadataDecoder
{
    public MetadataElement[]? AttMetadata;
    public MetadataElement? FileMetadata;

    public void Decode(DecoderBuffer buffer)
    {
        var num_att_metadata = (uint)buffer.DecodeVarIntUnsigned();
        AttMetadata = new MetadataElement[num_att_metadata];
        for (uint i = 0; i < num_att_metadata; ++i)
        {
            var id = (uint)buffer.DecodeVarIntUnsigned();
            AttMetadata[i] = DecodeMetadataElement(buffer);
            AttMetadata[i].Id = id;
        }
        FileMetadata = DecodeMetadataElement(buffer);
    }

    private static MetadataElement DecodeMetadataElement(DecoderBuffer buffer)
    {
        var numEntries = (uint)buffer.DecodeVarIntUnsigned();
        var metadata = new MetadataElement
        {
            Keys = new sbyte[numEntries][],
            Values = new sbyte[numEntries][]
        };
        for (uint i = 0; i < numEntries; ++i)
        {
            var keySize = buffer.ReadByte();
            var key = buffer.ReadSBytes(keySize);
            metadata.Keys[i] = key;
            var valueSize = buffer.ReadByte();
            var value = buffer.ReadSBytes(valueSize);
            metadata.Values[i] = value;
        }
        var numSubMetadata = (uint)buffer.DecodeVarIntUnsigned();
        for (uint i = 0; i < numSubMetadata; ++i)
        {
            var keySize = buffer.ReadByte();
            metadata.SubMetadataKeys[i] = buffer.ReadSBytes(keySize);
            metadata.SubMetadata![i] = DecodeMetadataElement(buffer);
        }
        return metadata;
    }
}
