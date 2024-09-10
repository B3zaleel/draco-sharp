using System.Numerics;
using Draco.IO.Enums;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class PredictionSchemeWrapTransform<TDataType> : PredictionSchemeWrapTransform<TDataType, TDataType>
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

internal class PredictionSchemeWrapTransform<TDataType, TCorrectedType>
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
    private readonly List<TDataType> _clampedValue = [];
    private TDataType One { get => (TDataType)Convert.ChangeType(1, typeof(TDataType)); }
    private TDataType Zero { get => (TDataType)Convert.ChangeType(0, typeof(TDataType)); }
    public PredictionSchemeTransformType Type { get => PredictionSchemeTransformType.Wrap; }
    public TDataType MinValue { get; set; }
    public TDataType MaxValue { get; set; }
    public TDataType MaxDiff { get; private set; }
    public TDataType MinCorrection { get; private set; }
    public TDataType MaxCorrection { get; private set; }

    public void Init(int componentsCount)
    {
        ComponentsCount = componentsCount;
        _clampedValue.Resize(ComponentsCount, (TDataType)default!);
    }

    public bool AreCorrectionsPositive()
    {
        return false;
    }

    public TDataType[] ClampPredictedValue(TDataType[] predictedValue)
    {
        var clampedValue = new TDataType[ComponentsCount];
        for (int i = 0; i < ComponentsCount; ++i)
        {
            if (predictedValue[i] > MaxValue)
            {
                clampedValue[i] = MaxValue;
            }
            else if (predictedValue[i] < MinValue)
            {
                clampedValue[i] = MinValue;
            }
            else
            {
                clampedValue[i] = predictedValue[i];
            }
        }
        return clampedValue;
    }

    protected void InitCorrectionBounds()
    {
        var diff = MaxValue - MinValue;
        Assertions.ThrowIf(diff < Zero || diff >= TDataType.MaxValue);
        MaxDiff = One + diff;
        MaxCorrection = MaxDiff / (One + One);
        MinCorrection = Zero - MaxCorrection;

        if ((MaxDiff & One) == Zero)
        {
            MaxCorrection -= One;
        }
    }
}
