using Draco.IO.Core;
using Draco.IO.Enums;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes.PredictionSchemes;

internal abstract class MeshPredictionSchemeGeometricNormalPredictor(MeshPredictionSchemeData predictionSchemeData)
{
    public PointAttribute? PositionAttribute { get; set; }
    public List<uint>? EntryToPointIdMap { get; set; }
    protected MeshPredictionSchemeData MeshData { get; set; } = predictionSchemeData;
    public NormalPredictionMode Mode { get; set; }

    public bool IsInitialized()
    {
        return PositionAttribute != null && EntryToPointIdMap != null;
    }

    protected Vector3<long> GetPositionForDataId(uint dataId)
    {
        Assertions.ThrowIfNot(IsInitialized());
        var pointId = EntryToPointIdMap![(int)dataId];
        var posValueId = PositionAttribute!.MappedIndex(pointId);
        return new Vector3<long>(PositionAttribute.ConvertValue<long>(posValueId));
    }

    protected Vector3<long> GetPositionForCorner(uint cornerId)
    {
        Assertions.ThrowIfNot(IsInitialized());
        var vertexId = MeshData.CornerTable!.Vertex(cornerId);
        return GetPositionForDataId((uint)MeshData.VertexToDataMap![(int)vertexId]);
    }

    protected Vector2<int> GetOctahedralCoordForDataId(uint dataId, int[] data)
    {
        Assertions.ThrowIfNot(IsInitialized());
        var dataOffset = dataId * 2;
        return new Vector2<int>(data[dataOffset], data[dataOffset + 1]);
    }

    public abstract Vector<int> ComputePredictedValue(uint cornerId);
}
