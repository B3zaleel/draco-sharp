using System.Numerics;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes.PredictionSchemes;

internal abstract class PredictionSchemeNormalOctahedronTransform<TDataType> : PredictionSchemeNormalOctahedronTransform<TDataType, TDataType>
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

internal abstract class PredictionSchemeNormalOctahedronTransform<TDataType, TCorrectedType>
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
    private int _maxQuantizedValue;
    protected readonly OctahedronToolBox _octahedronToolBox = new();

    public int MaxQuantizedValue
    {
        get => _maxQuantizedValue;
        protected set
        {
            Assertions.ThrowIf(value % 2 == 0);
            _maxQuantizedValue = value;
            _octahedronToolBox.SetQuantizationBits((byte)(BitUtilities.MostSignificantBit((uint)_maxQuantizedValue) + 1));
        }
    }
    public int CenterValue { get => _octahedronToolBox.CenterValue; }

    protected bool IsInDiamond(int s, int t)
    {
        return _octahedronToolBox.IsInDiamond(s, t);
    }

    protected void InvertDiamond(ref int s, ref int t)
    {
        _octahedronToolBox.InvertDiamond(ref s, ref t);
    }

    protected int ModMax(int x)
    {
        return _octahedronToolBox.ModMax(x);
    }

    protected int MakePositive(int x)
    {
        return _octahedronToolBox.MakePositive(x);
    }
}
