using System.Numerics;
using Draco.IO.Enums;

namespace Draco.IO.Attributes.PredictionSchemes;

internal interface IPredictionSchemeEncodingTransform<TDataType, TCorrectedType>
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
    public int ComponentsCount { get; set; }
    public int QuantizationBits { get; }
    public PredictionSchemeTransformType Type { get => PredictionSchemeTransformType.Delta; }

    public virtual void Init(TDataType[] originalData, int size, int componentsCount)
    {
        ComponentsCount = componentsCount;
    }

    public virtual TCorrectedType[] ComputeCorrectionValue(TDataType[] originalValues, TDataType[] predictedValues)
    {
        var correctionValues = new TCorrectedType[ComponentsCount];

        for (int i = 0; i < ComponentsCount; ++i)
        {
            correctionValues[i] = (TCorrectedType)Convert.ChangeType(originalValues[i] - predictedValues[i], typeof(TCorrectedType))!;
        }
        return correctionValues;
    }

    public virtual void EncodeTransformData(EncoderBuffer encoderBuffer) { }

    public virtual bool AreCorrectionsPositive()
    {
        return false;
    }
}
