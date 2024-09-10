using System.Numerics;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class PredictionSchemeWrapEncodingTransform<TDataType> : PredictionSchemeWrapEncodingTransform<TDataType, TDataType>
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

internal class PredictionSchemeWrapEncodingTransform<TDataType, TCorrectedType> : PredictionSchemeWrapTransform<TDataType, TCorrectedType>, IPredictionSchemeEncodingTransform<TDataType, TCorrectedType>
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
    public int QuantizationBits { get; } = -1;

    public void Init(TDataType[] originalData, int size, int componentsCount)
    {
        Init(componentsCount);
        if (size == 0)
        {
            return;
        }
        var minValue = originalData[0];
        var maxValue = minValue;

        for (int i = 1; i < size; ++i)
        {
            if (originalData[i] < minValue)
            {
                minValue = originalData[i];
            }
            else if (originalData[i] > maxValue)
            {
                maxValue = originalData[i];
            }
        }
        MinValue = minValue;
        MaxValue = maxValue;
        InitCorrectionBounds();
    }

    public TCorrectedType[] ComputeCorrection(TDataType[] originalValues, TDataType[] predictedValues)
    {
        var correctedValues = new TCorrectedType[ComponentsCount];

        for (int i = 0; i < ComponentsCount; ++i)
        {
            predictedValues = ClampPredictedValue(predictedValues);
            correctedValues[i] = Constants.ConstCast<TDataType, TCorrectedType>(originalValues[i] - predictedValues[i]);

            if (Constants.ConstCast<TCorrectedType, TDataType>(correctedValues[i]) < MinCorrection)
            {
                correctedValues[i] += Constants.ConstCast<TDataType, TCorrectedType>(MaxDiff);
            }
            else if (Constants.ConstCast<TCorrectedType, TDataType>(correctedValues[i]) > MaxCorrection)
            {
                correctedValues[i] -= Constants.ConstCast<TDataType, TCorrectedType>(MaxDiff);
            }
        }
        return correctedValues;
    }

    public void EncodeTransformData(EncoderBuffer encoderBuffer)
    {
        encoderBuffer.Write(MinValue);
        encoderBuffer.Write(MaxValue);
    }
}
