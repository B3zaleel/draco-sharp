using System.Numerics;
using Draco.IO.Enums;
using Draco.IO.Extensions;
using Draco.IO.Mesh;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class MeshPredictionSchemeParallelogramDecoder<TDataType, TTransform>(PointAttribute attribute, TTransform transform, MeshPredictionSchemeData meshData) : MeshPredictionSchemeDecoder<TDataType, TTransform>(attribute, transform, meshData)
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
    where TTransform : IPredictionSchemeDecodingTransform<TDataType, TDataType>
{
    public override PredictionSchemeMethod Method { get => PredictionSchemeMethod.Parallelogram; }

    public bool IsInitialized()
    {
        return MeshData.IsInitialized();
    }

    public override TDataType[] ComputeOriginalValues(TDataType[] correctedData, int size, int numComponents, List<uint> entryToPointMap)
    {
        Transform.Init(numComponents);
        var originalValues = new TDataType[size * numComponents];
        var predictedValues = new TDataType[numComponents];
        var zero = (TDataType)Convert.ChangeType(0, typeof(TDataType));
        Array.Fill(predictedValues, zero);
        originalValues.SetSubArray(Transform.ComputeOriginalValue(predictedValues.GetSubArray(0), correctedData.GetSubArray(0, numComponents)), 0);

        for (int p = 1; p < MeshData.DataToCornerMap!.Count; ++p)
        {
            var cornerId = MeshData.DataToCornerMap[p];
            var dstOffset = p * numComponents;

            if (TryComputeParallelogramPrediction(p, cornerId, MeshData.CornerTable!, MeshData.VertexToDataMap!, originalValues, numComponents, out TDataType[] predictedData))
            {
                originalValues.SetSubArray(Transform.ComputeOriginalValue(predictedData, correctedData.GetSubArray(dstOffset, numComponents)), dstOffset);
            }
            else
            {
                var srcOffset = (p - 1) * numComponents;
                originalValues.SetSubArray(Transform.ComputeOriginalValue(originalValues.GetSubArray(srcOffset), correctedData.GetSubArray(dstOffset, numComponents)), dstOffset);
            }
        }
        return originalValues;
    }

    private static (int OppositeEntry, int NextEntry, int PreviousEntry) GetParallelogramEntries(uint cornerId, CornerTable table, List<int> vertexToDataMap)
    {
        return (vertexToDataMap[(int)table.Vertex(cornerId)], vertexToDataMap[(int)table.Vertex(table.Next(cornerId))], vertexToDataMap[(int)table.Vertex(table.Previous(cornerId))]);
    }

    internal static bool TryComputeParallelogramPrediction<T>(int dataEntryId, uint cornerId, CornerTable table, List<int> vertexToDataMap, T[] data, int numComponents, out T[] predictedData)
        where T : struct,
            IAdditionOperators<T, T, T>,
            ISubtractionOperators<T, T, T>
    {
        var oppositeCornerId = table.Opposite(cornerId);
        predictedData = [];

        if (oppositeCornerId == Constants.kInvalidCornerIndex)
        {
            return false;
        }
        var (oppositeVertex, nextVertex, previousVertex) = GetParallelogramEntries(oppositeCornerId, table, vertexToDataMap);

        if (oppositeVertex < dataEntryId && nextVertex < dataEntryId && previousVertex < dataEntryId)
        {
            var oppositeVertexOffset = oppositeVertex * numComponents;
            var nextVertexOffset = nextVertex * numComponents;
            var previousVertexOffset = previousVertex * numComponents;
            predictedData = new T[numComponents];

            for (int c = 0; c < numComponents; ++c)
            {
                predictedData[c] = data[nextVertexOffset + c] + data[previousVertexOffset + c] - data[oppositeVertexOffset + c];
            }
            return true;
        }
        return false;
    }
}
