using System.Numerics;
using Draco.IO.Core;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class MeshPredictionSchemeTexCoordsPortablePredictor<TDataType>(MeshPredictionSchemeData predictionSchemeData, bool isEncoding)
    where TDataType : struct,
        IComparisonOperators<TDataType, TDataType, bool>,
        IComparable,
        IEqualityOperators<TDataType, TDataType, bool>,
        IAdditionOperators<TDataType, TDataType, TDataType>,
        ISubtractionOperators<TDataType, TDataType, TDataType>,
        IDivisionOperators<TDataType, TDataType, TDataType>,
        IMultiplyOperators<TDataType, TDataType, TDataType>,
        IDecrementOperators<TDataType>,
        IBitwiseOperators<TDataType, TDataType, TDataType>,
        IMinMaxValue<TDataType>
{
    public const int kNumComponents = 2;
    private readonly bool _isEncoding = isEncoding;
    public PointAttribute? PositionAttribute { get; set; }
    public TDataType[] PredictedValue { get; private set; } = new TDataType[kNumComponents];
    public List<uint> EntryToPointMap { get; set; } = [];
    public List<bool> Orientations { get; set; } = [];
    public MeshPredictionSchemeData MeshData { get; } = predictionSchemeData;

    public bool IsInitialized()
    {
        return PositionAttribute != null;
    }

    public Vector3<long> GetPositionForEntryId(int entryId)
    {
        var pointIndex = EntryToPointMap[entryId];
        var pos = new Vector3<long>(PositionAttribute!.ConvertValue<TDataType>(PositionAttribute.MappedIndex(pointIndex), 3).Select(val => Constants.ConstCast<TDataType, long>(val)).ToArray());
        return pos;
    }

    public Vector2<long> GetTexCoordForEntryId(int entryId, TDataType[] data)
    {
        var dataOffset = entryId * kNumComponents;
        return new Vector2<long>(Constants.ConstCast<TDataType, long>(data[dataOffset]), Constants.ConstCast<TDataType, long>(data[dataOffset + 1]));
    }

    public void ComputePredictedValue(uint cornerId, TDataType[] data, int dataId)
    {
        var nextCornerId = MeshData.CornerTable!.Next(cornerId);
        var prevCornerId = MeshData.CornerTable.Previous(cornerId);
        var nextVertId = MeshData.CornerTable.Vertex(nextCornerId);
        var prevVertId = MeshData.CornerTable.Vertex(prevCornerId);
        var nextDataId = MeshData.VertexToDataMap![(int)nextVertId];
        var prevDataId = MeshData.VertexToDataMap![(int)prevVertId];

        if (prevDataId < dataId && nextDataId < dataId)
        {
            var nextUV = GetTexCoordForEntryId(nextDataId, data);
            var prevUV = GetTexCoordForEntryId(prevDataId, data);

            if (prevUV == nextUV)
            {
                PredictedValue[0] = Constants.ConstCast<long, TDataType>(prevUV[0]);
                PredictedValue[1] = Constants.ConstCast<long, TDataType>(prevUV[1]);
                return;
            }
            var tipPos = GetPositionForEntryId(dataId);
            var nextPos = GetPositionForEntryId(nextDataId);
            var prevPos = GetPositionForEntryId(prevDataId);
            var pn = prevPos - nextPos;
            var pnNorm2Squared = pn.SquaredNorm();

            if (pnNorm2Squared != 0)
            {
                var cn = tipPos - nextPos;
                var cnDotPn = pn.Dot(cn);
                var cnDotPnAsLong = (long)Convert.ChangeType(cnDotPn, typeof(long));
                var pnUV = prevUV - nextUV;
                var nUVAbsMaxElement = Math.Max(MathUtilities.Abs(nextUV[0]), MathUtilities.Abs(nextUV[1]));
                Assertions.ThrowIf(nUVAbsMaxElement > long.MaxValue / (long)Convert.ChangeType(pnNorm2Squared, typeof(long)));
                var pnUVAbsMaxElement = Math.Max(MathUtilities.Abs(pnUV[0]), MathUtilities.Abs(pnUV[1]));
                Assertions.ThrowIf(cnDotPnAsLong > long.MaxValue / pnUVAbsMaxElement);
                var xUV = nextUV * pnNorm2Squared + (cnDotPn * pnUV);
                var pnAbsMaxElement = Math.Max(Math.Max(MathUtilities.Abs(pn[0]), MathUtilities.Abs(pn[1])), MathUtilities.Abs(pn[2]));
                Assertions.ThrowIf(cnDotPnAsLong > long.MaxValue / (long)Convert.ChangeType(pnAbsMaxElement, typeof(long)));
                var xPos = nextPos + (cnDotPn * pn) / pnNorm2Squared;
                var cxNorm2Squared = (tipPos - xPos).SquaredNorm();
                var cxUV = new Core.Vector<long>(pnUV[1], -pnUV[0]);
                var normSquared = MathUtilities.IntSqrt(cxNorm2Squared * pnNorm2Squared);
                cxUV *= normSquared;
                Vector2<long> predictedUV;

                if (_isEncoding)
                {
                    var predictedUV0 = (xUV + cxUV) / pnNorm2Squared;
                    var predictedUV1 = (xUV - cxUV) / pnNorm2Squared;
                    var cUV = GetTexCoordForEntryId(dataId, data);

                    if ((cUV - predictedUV0).SquaredNorm() < (cUV - predictedUV1).SquaredNorm())
                    {
                        predictedUV = new Vector2<long>(predictedUV0);
                        Orientations.Add(true);
                    }
                    else
                    {
                        predictedUV = new Vector2<long>(predictedUV1);
                        Orientations.Add(false);
                    }
                }
                else
                {
                    Assertions.ThrowIf(Orientations.Count == 0);
                    var orientation = Orientations.Last();
                    Orientations.PopBack();
                    predictedUV = new Vector2<long>(orientation ? (xUV + cxUV) / pnNorm2Squared : (xUV - cxUV) / pnNorm2Squared);
                }
                PredictedValue[0] = Constants.ConstCast<long, TDataType>(predictedUV[0]);
                PredictedValue[1] = Constants.ConstCast<long, TDataType>(predictedUV[1]);
                return;
            }
        }
        int dataOffset = 0;

        if (prevDataId < dataId)
        {
            dataOffset = prevDataId * kNumComponents;
        }
        if (nextDataId < dataId)
        {
            dataOffset = nextDataId * kNumComponents;
        }
        else
        {
            if (dataId > 0)
            {
                dataOffset = (dataId - 1) * kNumComponents;
            }
            else
            {
                for (int i = 0; i < kNumComponents; ++i)
                {
                    PredictedValue[i] = default;
                }
                return;
            }
        }
        for (int i = 0; i < kNumComponents; ++i)
        {
            PredictedValue[i] = data[dataOffset + i];
        }
    }
}
