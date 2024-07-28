using Draco.IO.Attributes;

namespace Draco.IO.Mesh.Traverser;

internal class MeshAttributeIndicesEncodingObserver(CornerTable cornerTable, Mesh mesh, MeshAttributeIndicesEncodingData encodingData, PointsSequencer sequencer) : TraversalObserver
{
    private readonly CornerTable _cornerTable = cornerTable;
    private readonly Mesh _mesh = mesh;
    private readonly MeshAttributeIndicesEncodingData _encodingData = encodingData;
    private readonly PointsSequencer _sequencer = sequencer;

    public override void OnNewFaceVisited(uint face) { }

    public override void OnNewVertexVisited(uint vertex, uint corner)
    {
        var pointId = _mesh.GetFace(corner / 3)[corner % 3];
        _sequencer.AddPointId((uint)pointId);
        _encodingData.EncodedAttributeValueIndexToCornerMap.Add(corner);
        _encodingData.VertexToEncodedAttributeValueIndexMap[(int)vertex] = _encodingData.NumValues;
        _encodingData.NumValues++;
    }
}
