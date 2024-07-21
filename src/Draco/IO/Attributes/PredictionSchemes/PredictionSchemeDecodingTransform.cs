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
    protected int ComponentsCount { get; set; }

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
