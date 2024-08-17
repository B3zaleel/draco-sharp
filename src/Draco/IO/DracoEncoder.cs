using Draco.IO.Metadata;

namespace Draco.IO;

public class DracoEncoder
{
    public void Encode(string path, Draco draco)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        Encode(new BinaryWriter(fs), draco);
    }

    public void Encode(Stream stream, Draco draco)
    {
        Encode(new BinaryWriter(stream), draco);
    }

    public void Encode(BinaryWriter binaryWriter, Draco draco)
    {
        using var encoderBuffer = new EncoderBuffer(binaryWriter);
        EncodeHeader(encoderBuffer, draco.Header);
        encoderBuffer.BitStreamVersion = draco.Header.Version;

        if (draco.Header.Version >= Constants.BitStreamVersion(1, 3) && (draco.Header.Flags & Constants.Metadata.FlagMask) == Constants.Metadata.FlagMask)
        {
            MetadataEncoder.Encode(encoderBuffer, draco.Metadata!);
        }
    }

    private static void EncodeHeader(EncoderBuffer encoderBuffer, DracoHeader header)
    {
        encoderBuffer.WriteASCIIBytes(Constants.DracoMagic);
        encoderBuffer.WriteByte(header.MajorVersion);
        encoderBuffer.WriteByte(header.MinorVersion);
        encoderBuffer.WriteByte(header.EncoderType);
        encoderBuffer.WriteByte(header.EncoderMethod);
        encoderBuffer.WriteUInt16(header.Flags);
    }
}
