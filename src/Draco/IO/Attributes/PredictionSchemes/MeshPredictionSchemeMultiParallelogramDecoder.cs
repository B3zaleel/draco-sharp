using System.Numerics;
using Draco.IO.Core;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class MeshPredictionSchemeMultiParallelogramDecoder<TDataType, TTransform>(PointAttribute attribute, TTransform transform, MeshPredictionSchemeData meshData) : MeshPredictionSchemeDecoder<TDataType, TTransform>(attribute, transform, meshData)
    where TDataType : struct,
        IComparisonOperators<TDataType, TDataType, bool>,
        IComparable,
        IEqualityOperators<TDataType, TDataType, bool>,
        IAdditionOperators<TDataType, TDataType, TDataType>,
        ISubtractionOperators<TDataType, TDataType, TDataType>,
        IDivisionOperators<TDataType, TDataType, TDataType>,
        IDecrementOperators<TDataType>,
        IBitwiseOperators<TDataType, TDataType, TDataType>,
        IMinMaxValue<TDataType>
    where TTransform : PredictionSchemeDecodingTransform<TDataType>
{
    public override PredictionSchemeMethod Method { get => PredictionSchemeMethod.MultiParallelogram; }

    public override TDataType[] ComputeOriginalValues(TDataType[] correctedData, int size, int numComponents, List<uint> entryToPointMap)
    {
        Transform.Init(numComponents);
        var originalValues = new TDataType[size * numComponents];
        var predictedValues = new TDataType[numComponents];
        var zero = (TDataType)Convert.ChangeType(0, typeof(TDataType));
        Array.Fill(predictedValues, zero);
        originalValues.SetSubArray(Transform.ComputeOriginalValue(originalValues.GetSubArray(0), correctedData.GetSubArray(0)), 0);

        for (int p = 1; p < MeshData.DataToCornerMap!.Count; ++p)
        {
            var startCornerId = MeshData.DataToCornerMap[p];
            var cornerId = startCornerId;
            int numParallelograms = 0;

            while (cornerId != Constants.kInvalidCornerIndex)
            {
                if (MeshPredictionSchemeParallelogramDecoder<TDataType, TTransform>.TryComputeParallelogramPrediction(p, cornerId, MeshData.CornerTable!, MeshData.VertexToDataMap!, originalValues, numComponents, out TDataType[] parallelogramPredictedValues))
                {
                    for (int c = 0; c < numComponents; ++c)
                    {
                        predictedValues[c] = MathUtilities.AddAsUnsigned(predictedValues[c], parallelogramPredictedValues[c]);
                    }
                    ++numParallelograms;
                }
                cornerId = MeshData.CornerTable!.SwingRight(cornerId);

                if (cornerId == startCornerId)
                {
                    cornerId = Constants.kInvalidCornerIndex;
                }
            }
            var dstOffset = p * numComponents;

            if (numParallelograms == 0)
            {
                var srcOffset = (p - 1) * numComponents;
                originalValues.SetSubArray(Transform.ComputeOriginalValue(originalValues.GetSubArray(srcOffset), correctedData.GetSubArray(dstOffset)), dstOffset);
            }
            else
            {
                for (int c = 0; c < numComponents; ++c)
                {
                    predictedValues[c] /= (TDataType)Convert.ChangeType(numParallelograms, typeof(TDataType));
                }
                originalValues.SetSubArray(Transform.ComputeOriginalValue(predictedValues, correctedData.GetSubArray(dstOffset)), dstOffset);
            }
        }
        return originalValues;
    }
}
