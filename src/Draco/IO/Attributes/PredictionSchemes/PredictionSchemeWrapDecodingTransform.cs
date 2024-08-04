using System.Numerics;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class PredictionSchemeWrapDecodingTransform<TDataType> : PredictionSchemeWrapDecodingTransform<TDataType, TDataType>
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
{ }

internal class PredictionSchemeWrapDecodingTransform<TDataType, TCorrectedType> : PredictionSchemeWrapTransform<TDataType, TCorrectedType>
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
    where TCorrectedType : struct,
        IComparisonOperators<TCorrectedType, TCorrectedType, bool>,
        IComparable,
        IEqualityOperators<TCorrectedType, TCorrectedType, bool>,
        IAdditionOperators<TCorrectedType, TCorrectedType, TCorrectedType>,
        ISubtractionOperators<TCorrectedType, TCorrectedType, TCorrectedType>,
        IDivisionOperators<TCorrectedType, TCorrectedType, TCorrectedType>,
        IMultiplyOperators<TCorrectedType, TCorrectedType, TCorrectedType>,
        IDecrementOperators<TCorrectedType>,
        IBitwiseOperators<TCorrectedType, TCorrectedType, TCorrectedType>,
        IMinMaxValue<TCorrectedType>
{
    public override TDataType[] ComputeOriginalValue(TDataType[] predictedValues, TCorrectedType[] correctedValues)
    {
        var originalValues = new TDataType[ComponentsCount];
        predictedValues = ClampPredictedValue(predictedValues);
        var predictedValuesAsUint = Constants.ReinterpretCast<TDataType, uint>(predictedValues);
        var correctedValuesAsUint = Constants.ReinterpretCast<TCorrectedType, uint>(correctedValues);

        for (int i = 0; i < ComponentsCount; ++i)
        {
            originalValues[i] = (TDataType)Convert.ChangeType(predictedValuesAsUint[i] + correctedValuesAsUint[i], typeof(TDataType))!;

            if (originalValues[i] > MaxValue)
            {
                originalValues[i] -= MaxDiff;
            }
            else if (originalValues[i] < MinValue)
            {
                originalValues[i] += MaxDiff;
            }
        }
        return originalValues;
    }

    public override void DecodeTransformData(DecoderBuffer decoderBuffer)
    {
        MinValue = decoderBuffer.Read<TDataType>();
        MaxValue = decoderBuffer.Read<TDataType>();
        Assertions.ThrowIf(MinValue > MaxValue);
        InitCorrectionBounds();
    }
}
