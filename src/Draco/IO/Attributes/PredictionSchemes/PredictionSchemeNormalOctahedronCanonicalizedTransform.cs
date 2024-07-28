using System.Numerics;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class PredictionSchemeNormalOctahedronCanonicalizedTransform<TDataType> : PredictionSchemeNormalOctahedronTransform<TDataType>
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
    public int GetRotationCount(int[] p)
    {
        var signX = p[0];
        var signY = p[1];
        int rotationCount;

        if (signX == 0)
        {
            if (signY == 0)
            {
                rotationCount = 0;
            }
            else
            {
                rotationCount = signY > 0 ? 3 : 1;
            }
        }
        else if (signX > 0)
        {
            rotationCount = signY >= 0 ? 2 : 1;
        }
        else
        {
            rotationCount = signY <= 0 ? 0 : 3;
        }
        return rotationCount;
    }

    public int[] RotatePoint(int[] p, int rotationCount)
    {
        return rotationCount switch
        {
            1 => [p[1], -p[0]],
            2 => [-p[0], -p[1]],
            3 => [-p[1], p[0]],
            _ => p
        };
    }

    public bool IsInBottomLeft(int[] p)
    {
        if (p[0] == 0 && p[1] == 0)
        {
            return true;
        }
        return p[0] < 0 && p[1] <= 0;
    }
}