using Draco.IO.Metadata;

namespace Draco.IO;

public class DracoDecoder
{
    public DracoHeader? Header { get; private set; }
    public MetadataElement[]? AttMetadata;
    public MetadataElement? FileMetadata;

    public void Decode(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        Decode(new BinaryReader(fs));
    }

    public void Decode(Stream stream)
    {
        Decode(new BinaryReader(stream));
    }

    public void Decode(BinaryReader binaryReader)
    {
        using var buffer = new DecoderBuffer(binaryReader);
        Header = ParseHeader(buffer);
        buffer.BitStream_Version = Header.Version;
        if (Header.Version >= Constants.BitStreamVersion(1, 3) && (Header.Flags & Constants.Metadata.FlagMask) == Constants.Metadata.FlagMask)
        {
            var metadataDecoder = new MetadataDecoder();
            metadataDecoder.Decode(buffer);
            AttMetadata = metadataDecoder.AttMetadata;
            FileMetadata = metadataDecoder.FileMetadata;
        }
    }

    private static DracoHeader ParseHeader(DecoderBuffer buffer)
    {
        var dracoMagic = buffer.ReadASCIIBytes(Constants.DracoMagic.Length);
        if (dracoMagic != Constants.DracoMagic)
        {
            throw new InvalidDataException("Invalid Draco file.");
        }
        var majorVersion = buffer.ReadByte();
        var minorVersion = buffer.ReadByte();
        var encoderType = buffer.ReadByte();
        var encoderMethod = buffer.ReadByte();
        var flags = buffer.ReadUInt16();

        return new DracoHeader(
            majorVersion: majorVersion,
            minorVersion: minorVersion,
            encoderType: encoderType,
            encoderMethod: encoderMethod,
            flags: flags
        );
    }
}