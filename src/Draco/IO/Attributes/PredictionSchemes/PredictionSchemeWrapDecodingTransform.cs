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
        IDecrementOperators<TDataType>,
        IBitwiseOperators<TDataType, TDataType, TDataType>,
        IMinMaxValue<TDataType>
{ }

internal class PredictionSchemeWrapDecodingTransform<TDataType, TCorrectedType> : PredictionSchemeWrapTransform<TDataType>
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
    where TCorrectedType : struct,
        IComparisonOperators<TDataType, TDataType, bool>,
        IComparable,
        IEqualityOperators<TDataType, TDataType, bool>,
        IAdditionOperators<TDataType, TDataType, TDataType>,
        ISubtractionOperators<TDataType, TDataType, TDataType>,
        IDivisionOperators<TDataType, TDataType, TDataType>,
        IDecrementOperators<TDataType>,
        IBitwiseOperators<TDataType, TDataType, TDataType>,
        IMinMaxValue<TDataType>
{
    public TDataType[] ComputeOriginalValue(TDataType[] predictedValues, TCorrectedType[] correctedValues)
    {
        var originalValues = new TDataType[ComponentsCount];
        predictedValues = ClampPredictedValue(predictedValues);

        for (int i = 0; i < ComponentsCount; ++i)
        {
            originalValues[i] = predictedValues[i] + (TDataType)Convert.ChangeType(correctedValues[i], typeof(TDataType))!;

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
