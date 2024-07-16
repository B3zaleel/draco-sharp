namespace Draco.IO;

internal interface IConnectivityDecoder
{
    public int GeometryType { get; }

    public void DecodeConnectivity(DecoderBuffer decoderBuffer);
}
