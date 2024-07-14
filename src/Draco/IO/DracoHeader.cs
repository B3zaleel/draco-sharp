namespace Draco.IO;

public class DracoHeader(byte majorVersion, byte minorVersion, byte encoderType, byte encoderMethod, ushort flags)
{
    public byte MajorVersion { get; } = majorVersion;
    public byte MinorVersion { get; } = minorVersion;
    public ushort Version { get; } = Constants.BitStreamVersion(majorVersion, minorVersion);
    public byte EncoderType { get; } = encoderType;
    public byte EncoderMethod { get; } = encoderMethod;
    public ushort Flags { get; } = flags;
}
