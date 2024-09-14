using Draco.IO;

namespace Draco;

public class DracoHeader
{
    public byte MajorVersion { get; }
    public byte MinorVersion { get; }
    public ushort Version { get; }
    public byte EncoderType { get; }
    public byte EncoderMethod { get; }
    public ushort Flags { get; }

    internal DracoHeader(byte majorVersion, byte minorVersion, byte encoderType, byte encoderMethod, ushort flags)
    {
        MajorVersion = majorVersion;
        MinorVersion = minorVersion;
        Version = Constants.BitStreamVersion(majorVersion, minorVersion);
        EncoderType = encoderType;
        EncoderMethod = encoderMethod;
        Flags = flags;
    }
}
