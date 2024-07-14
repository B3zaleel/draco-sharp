using Draco.IO;

namespace Draco.Examples;

public static class Program
{
    public static void Main()
    {
        DecodeFile("Samples/house_04.obj.drc");
    }

    public static void DecodeFile(string path)
    {
        var decoder = new DracoDecoder();
        decoder.Decode(path);
    }
}
