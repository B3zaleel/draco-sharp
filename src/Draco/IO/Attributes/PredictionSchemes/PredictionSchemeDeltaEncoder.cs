using System.Numerics;
using Draco.IO.Enums;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class PredictionSchemeDeltaEncoder<TDataType, TTransform>(PointAttribute attribute, TTransform transform) : PredictionSchemeEncoder<TDataType, TTransform>(attribute, transform)
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
    public override PredictionSchemeMethod Method { get => PredictionSchemeMethod.Difference; }

    public override TDataType[] ComputeCorrectionValues(TDataType[] data, int size, int numComponents, List<uint> entryToPointMap)
    {
        Transform.Init(data, size, numComponents);
        var correctionValues = new TDataType[size * numComponents];
        for (int i = size - numComponents; i > 0; i -= numComponents)
        {
            correctionValues.SetSubArray(Transform.ComputeCorrectionValue(data.GetSubArray(i), data.GetSubArray(i - numComponents)), i);
        }
        var zeroValues = new TDataType[numComponents];
        var zero = (TDataType)Convert.ChangeType(0, typeof(TDataType));
        Array.Fill(zeroValues, zero);
        correctionValues.SetSubArray(Transform.ComputeCorrectionValue(data.GetSubArray(0), zeroValues), 0);
        return correctionValues;
    }
}
