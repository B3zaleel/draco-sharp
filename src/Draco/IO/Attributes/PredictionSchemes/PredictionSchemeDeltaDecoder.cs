using System.Numerics;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class PredictionSchemeDeltaDecoder<TDataType, TTransform>(PointAttribute attribute, TTransform transform) : PredictionSchemeDecoder<TDataType, TTransform>(attribute, transform)
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
    public override PredictionSchemeMethod Method { get => PredictionSchemeMethod.Difference; }

    public override TDataType[] ComputeOriginalValues(TDataType[] correctedData, int size, int numComponents, List<uint> entryToPointMap)
    {
        Transform.Init(numComponents);
        var originalValues = new TDataType[size * numComponents];
        var zeroValues = new TDataType[numComponents];
        var zero = (TDataType)Convert.ChangeType(0, typeof(TDataType));
        Array.Fill(zeroValues, zero);
        originalValues.SetSubArray(Transform.ComputeOriginalValue(zeroValues.GetSubArray(0), correctedData.GetSubArray(0)), 0);

        for (int i = numComponents; i < size; i += numComponents)
        {
            originalValues.SetSubArray(Transform.ComputeOriginalValue(originalValues.GetSubArray(i - numComponents), correctedData.GetSubArray(i)), i);
        }
        return originalValues;
    }
}
