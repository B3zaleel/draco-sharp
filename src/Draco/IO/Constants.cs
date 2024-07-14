namespace Draco.IO;

internal static class Constants
{
    public const string DracoMagic = "DRACO";
    public const byte MajorVersion = 2;
    public const byte MinorVersion = 2;

    public static ushort BitStreamVersion(byte majorVersion, byte minorVersion) => (ushort)((majorVersion << 8) | minorVersion);
}
