using Draco.IO.Attributes;
using Draco.IO.Extensions;

namespace Draco.IO.Mesh.Traverser;

internal class MeshTraversalSequencer(Mesh mesh, MeshAttributeIndicesEncodingData encodingData) : PointsSequencer
{
    public Traverser? Traverser { get; set; }
    public Mesh Mesh { get; set; } = mesh;
    public MeshAttributeIndicesEncodingData EncodingData { get; set; } = encodingData;
    public List<uint> CornerOrders { get; set; } = [];

    protected override void GenerateSequenceInternal()
    {
        Traverser!.Start();
        if (CornerOrders?.Count > 0)
        {
            for (uint i = 0; i < CornerOrders.Count; ++i)
            {
                ProcessCorner(CornerOrders[(int)i]);
            }
        }
        else
        {
            for (uint i = 0; i < Traverser.CornerTable.FacesCount; ++i)
            {
                ProcessCorner(3 * i);
            }
        }
        Traverser.End();
    }

    public override void UpdatePointToAttributeIndexMapping(PointAttribute attribute)
    {
        attribute.SetExplicitMapping(Mesh.PointsCount);

        for (uint f = 0; f < Mesh.FacesCount; ++f)
        {
            var face = Mesh.GetFace(f);

            for (byte p = 0; p < 3; ++p)
            {
                var pointId = face[p];
                var vertexId = Traverser!.CornerTable.Vertex(3 * f + p);
                Assertions.ThrowIf(vertexId == Constants.kInvalidVertexIndex);
                var attributeEntryId = EncodingData.VertexToEncodedAttributeValueIndexMap[(int)vertexId];
                Assertions.ThrowIf(pointId >= Mesh.PointsCount || attributeEntryId >= Mesh.PointsCount, "There cannot be more attribute values than the number of points.");
                attribute.SetPointMapEntry((uint)pointId, (uint)attributeEntryId);
            }
        }
    }

    private void ProcessCorner(uint cornerId)
    {
        Traverser!.TraverseFromCorner(cornerId);
    }
}
