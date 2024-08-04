using System.Numerics;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class PredictionSchemeDecodingTransform<TDataType> : PredictionSchemeDecodingTransform<TDataType, TDataType>
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
{

}

internal class PredictionSchemeDecodingTransform<TDataType, TCorrectedType>
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
    protected int ComponentsCount { get; set; }
    public virtual int QuantizationBits { get; } = -1;

    public virtual void Init(int componentsCount)
    {
        ComponentsCount = componentsCount;
    }

    public TDataType[] ComputeOriginalValue(TDataType[] predictedValues, TCorrectedType[] correctedValues)
    {
        var originalValues = new TDataType[ComponentsCount];

        for (int i = 0; i < ComponentsCount; ++i)
        {
            originalValues[i] = predictedValues[i] + (TDataType)Convert.ChangeType(correctedValues[i], typeof(TDataType))!;
        }
        return originalValues;
    }

    public virtual void DecodeTransformData(DecoderBuffer decoderBuffer) { }

    public virtual bool AreCorrectionsPositive()
    {
        return false;
    }
}
