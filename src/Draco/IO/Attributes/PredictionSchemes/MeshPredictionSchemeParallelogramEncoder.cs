using System.Numerics;
using Draco.IO.Enums;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class MeshPredictionSchemeParallelogramEncoder<TDataType, TTransform>(PointAttribute attribute, TTransform transform, MeshPredictionSchemeData meshData) : MeshPredictionSchemeEncoder<TDataType, TTransform>(attribute, transform, meshData)
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
    public override PredictionSchemeMethod Method { get => PredictionSchemeMethod.Parallelogram; }

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

        for (int p = MeshData.DataToCornerMap!.Count - 1; p > 0; --p)
        {
            var cornerId = MeshData.DataToCornerMap[p];
            int dstOffset = p * numComponents;

            if (MeshPredictionSchemeParallelogramDecoder<TDataType, IPredictionSchemeDecodingTransform<TDataType, TDataType>>.TryComputeParallelogramPrediction(p, cornerId, table, MeshData.VertexToDataMap!, data, numComponents, out TDataType[] predictedData))
            {
                predictedValues.SetSubArray(predictedData, 0);
                int srcOffset = (p - 1) * numComponents;
                correctionValues.SetSubArray(Transform.ComputeCorrectionValue(data.GetSubArray(dstOffset, numComponents), data.GetSubArray(srcOffset, numComponents)), dstOffset);
            }
            else
            {
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
