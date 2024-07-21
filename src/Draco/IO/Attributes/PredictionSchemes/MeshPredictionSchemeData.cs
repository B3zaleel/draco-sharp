using Draco.IO.Mesh;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class MeshPredictionSchemeData
{
    public Mesh.Mesh? Mesh { get; set; }
    public CornerTable? CornerTable { get; set; }
    public List<uint>? DataToCornerMap { get; set; }
    public List<int>? VertexToDataMap { get; set; }

    public MeshPredictionSchemeData(Mesh.Mesh mesh, CornerTable cornerTable, List<uint> dataToCornerMap, List<int> vertexToDataMap)
    {
        Mesh = mesh;
        CornerTable = cornerTable;
        DataToCornerMap = dataToCornerMap;
        VertexToDataMap = vertexToDataMap;
    }

    public bool IsInitialized()
    {
        return Mesh != null && CornerTable != null && VertexToDataMap != null && DataToCornerMap != null;
    }
}
