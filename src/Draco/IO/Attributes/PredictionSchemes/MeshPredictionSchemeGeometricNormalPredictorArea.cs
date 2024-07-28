using Draco.IO.Core;
using Draco.IO.Extensions;
using Draco.IO.Mesh;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class MeshPredictionSchemeGeometricNormalPredictorArea : MeshPredictionSchemeGeometricNormalPredictor
{
    public MeshPredictionSchemeGeometricNormalPredictorArea(MeshPredictionSchemeData predictionSchemeData) : base(predictionSchemeData)
    {
        Mode = NormalPredictionMode.TriangleArea;
    }

    public override Vector<int> ComputePredictedValue(uint cornerId)
    {
        uint nextCorner, previousCorner;
        var centerPosition = GetPositionForCorner(cornerId);
        Vector<long> normal = new(3);

        foreach (var corner in new VertexCornersIterator(MeshData.CornerTable!, cornerId, false))
        {
            if (Mode == NormalPredictionMode.OneTriangle)
            {
                nextCorner = MeshData.CornerTable!.Next(cornerId);
                previousCorner = MeshData.CornerTable.Previous(cornerId);
            }
            else
            {
                nextCorner = MeshData.CornerTable!.Next(corner);
                previousCorner = MeshData.CornerTable.Previous(corner);
            }
            var nextPosition = GetPositionForCorner(nextCorner);
            var previousPosition = GetPositionForCorner(previousCorner);
            var deltaNext = nextPosition - centerPosition;
            var deltaPrevious = previousPosition - centerPosition;
            var cross = Vector<long>.CrossProduct(deltaNext, deltaPrevious);
            normal.Components[0] = (long)((ulong)normal.Components[0] + (ulong)cross.Components[0]);
            normal.Components[1] = (long)((ulong)normal.Components[1] + (ulong)cross.Components[1]);
            normal.Components[2] = (long)((ulong)normal.Components[2] + (ulong)cross.Components[2]);
        }
        long upperBound = 1 << 29;

        if (Mode == NormalPredictionMode.OneTriangle)
        {
            int absSum = (int)normal.AbsSum();

            if (absSum > upperBound)
            {
                normal /= absSum / upperBound;
            }
        }
        else
        {
            var absSum = (int)normal.AbsSum();

            if (absSum > upperBound)
            {
                normal /= absSum / upperBound;
            }
        }
        Assertions.ThrowIfNot(normal.AbsSum() <= upperBound);
        return new Vector<int>((int)normal[0], (int)normal[1], (int)normal[0]);
    }
}
