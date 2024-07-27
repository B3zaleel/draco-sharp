using Draco.IO.Attributes;

namespace Draco.IO;

internal abstract class ConnectivityDecoder
{
    public int BitStreamVersion { get; set; }
    public int GeometryType { get; }
    public PointCloud.PointCloud? PointCloud { get; }
    public List<AttributesDecoder?> AttributesDecoders { get; } = [];
    public List<int> AttributeToDecoderMap { get; } = [];

    public abstract void DecodeConnectivity(DecoderBuffer decoderBuffer);
}
