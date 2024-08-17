namespace Draco.IO.Metadata;

internal static class MetadataEncoder
{
    public static void Encode(EncoderBuffer encoderBuffer, DracoMetadata metadata)
    {
        encoderBuffer.EncodeVarIntUnsigned((uint)metadata.Attributes.Count);

        for (int i = 0; i < metadata.Attributes.Count; ++i)
        {
            encoderBuffer.EncodeVarIntUnsigned((uint)metadata.Attributes[i].Id!);
            EncodeMetadataElement(encoderBuffer, metadata.Attributes[i]);
        }
        EncodeMetadataElement(encoderBuffer, metadata.File);
    }

    private static void EncodeMetadataElement(EncoderBuffer encoderBuffer, MetadataElement metadataElement)
    {
        encoderBuffer.EncodeVarIntUnsigned((uint)metadataElement.Keys.Length);

        for (int i = 0; i < metadataElement.Keys.Length; ++i)
        {
            encoderBuffer.WriteByte((byte)metadataElement.Keys[i].Length);
            encoderBuffer.WriteSBytes(metadataElement.Keys[i]);
            encoderBuffer.WriteByte((byte)metadataElement.Values[i].Length);
            encoderBuffer.WriteSBytes(metadataElement.Values[i]);
        }
        encoderBuffer.EncodeVarIntUnsigned((uint)metadataElement.SubMetadataKeys.Length);
        for (int i = 0; i < metadataElement.SubMetadataKeys.Length; ++i)
        {
            encoderBuffer.WriteByte((byte)metadataElement.SubMetadataKeys[i].Length);
            encoderBuffer.WriteSBytes(metadataElement.SubMetadataKeys[i]);
            EncodeMetadataElement(encoderBuffer, metadataElement.SubMetadata![i]);
        }
    }
}
