using System.Numerics;
using Draco.IO.Enums;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class MeshPredictionSchemeMultiParallelogramEncoder<TDataType, TTransform>(PointAttribute attribute, TTransform transform, MeshPredictionSchemeData meshData) : MeshPredictionSchemeEncoder<TDataType, TTransform>(attribute, transform, meshData)
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
    where TTransform : IPredictionSchemeEncodingTransform<TDataType, TDataType>
{
    public override PredictionSchemeMethod Method { get => PredictionSchemeMethod.MultiParallelogram; }

    public bool IsInitialized()
    {
        return MeshData.IsInitialized();
    }

    public override TDataType[] ComputeCorrectionValues(TDataType[] data, int size, int numComponents, List<uint> entryToPointMap)
    {
        Transform.Init(data, size, numComponents);
        var table = MeshData.CornerTable!;
        var correctionValues = new TDataType[numComponents];
        var predictedValues = new TDataType[numComponents];
        var parallelogramPredictedValues = new TDataType[numComponents];

        for (int p = MeshData.DataToCornerMap!.Count - 1; p > 0; --p)
        {
            var startCornerId = MeshData.DataToCornerMap[p];
            var cornerId = startCornerId;
            int numParallelograms = 0;

            for (int i = 0; i < numComponents; ++i)
            {
                predictedValues[i] = Constants.ConstCast<int, TDataType>(0);
            }
            while (cornerId != Constants.kInvalidCornerIndex)
            {
                if (MeshPredictionSchemeParallelogramDecoder<TDataType, IPredictionSchemeDecodingTransform<TDataType, TDataType>>.TryComputeParallelogramPrediction(p, cornerId, table, MeshData.VertexToDataMap!, data, numComponents, out TDataType[] parallelogramPredictedData))
                {
                    parallelogramPredictedValues.SetSubArray(parallelogramPredictedData, 0);
                    for (int c = 0; c < numComponents; ++c)
                    {
                        predictedValues[c] += parallelogramPredictedValues[c];
                    }
                    ++numParallelograms;
                }
                cornerId = table.SwingRight(cornerId);
                if (cornerId == startCornerId)
                {
                    cornerId = Constants.kInvalidCornerIndex;
                }
            }
            int dstOffset = p * numComponents;
            if (numParallelograms == 0)
            {
                int srcOffset = (p - 1) * numComponents;
                correctionValues.SetSubArray(Transform.ComputeCorrectionValue(data.GetSubArray(dstOffset, numComponents), data.GetSubArray(srcOffset, numComponents)), dstOffset);
            }
            else
            {
                for (int c = 0; c < numComponents; ++c)
                {
                    predictedValues[c] /= Constants.ConstCast<int, TDataType>(numParallelograms);
                }
                correctionValues.SetSubArray(Transform.ComputeCorrectionValue(data.GetSubArray(dstOffset, numComponents), predictedValues), dstOffset);
            }
        }
        for (int i = 0; i < numComponents; ++i)
        {
            predictedValues[i] = Constants.ConstCast<int, TDataType>(0);
        }
        correctionValues.SetSubArray(Transform.ComputeCorrectionValue(data.GetSubArray(0, numComponents), predictedValues), 0);
        return correctionValues;
    }
}
