using System.Numerics;

namespace Draco.IO.Core;

internal class Vector<TScalar>
    where TScalar : struct,
        IComparisonOperators<TScalar, TScalar, bool>,
        IComparable,
        IEqualityOperators<TScalar, TScalar, bool>,
        IAdditionOperators<TScalar, TScalar, TScalar>,
        ISubtractionOperators<TScalar, TScalar, TScalar>,
        IDivisionOperators<TScalar, TScalar, TScalar>,
        IMultiplyOperators<TScalar, TScalar, TScalar>,
        IDecrementOperators<TScalar>,
        IBitwiseOperators<TScalar, TScalar, TScalar>,
        IMinMaxValue<TScalar>
{
    public byte Dimension { get; }
    public TScalar[] Components { get; private set; }
    public TScalar this[int index] { get => Components[index]; set => Components[index] = value; }

    public Vector(byte dimension)
    {
        Dimension = dimension;
        Components = new TScalar[dimension];

        for (int i = 0; i < Dimension; ++i)
        {
            Components[i] = default;
        }
    }

    public Vector(params TScalar[] components)
    {
        Dimension = (byte)components.Length;
        Components = components;
    }

    public Vector(Vector<TScalar> otherVector)
    {
        Dimension = otherVector.Dimension;
        Components = new TScalar[Dimension];
        Array.Copy(otherVector.Components, Components, Dimension);
    }

    public static Vector<TScalar> operator *(Vector<TScalar> left, Vector<TScalar> right)
    {
        if (left.Dimension != right.Dimension)
        {
            throw new InvalidOperationException("Cannot add vectors of different dimensions.");
        }
        var result = new Vector<TScalar>(left.Dimension);

        for (int i = 0; i < left.Dimension; ++i)
        {
            result.Components[i] = left.Components[i] * right.Components[i];
        }
        return result;
    }

    public static Vector<TScalar> operator +(Vector<TScalar> left, Vector<TScalar> right)
    {
        if (left.Dimension != right.Dimension)
        {
            throw new InvalidOperationException("Cannot add vectors of different dimensions.");
        }
        var result = new Vector<TScalar>(left.Dimension);

        for (int i = 0; i < left.Dimension; ++i)
        {
            result.Components[i] = left.Components[i] + right.Components[i];
        }
        return result;
    }

    public static Vector<TScalar> operator -(Vector<TScalar> left, Vector<TScalar> right)
    {
        if (left.Dimension != right.Dimension)
        {
            throw new InvalidOperationException("Cannot subtract vectors of different dimensions.");
        }
        var result = new Vector<TScalar>(left.Dimension);

        for (int i = 0; i < left.Dimension; ++i)
        {
            result.Components[i] = left.Components[i] - right.Components[i];
        }
        return result;
    }

    public static Vector<TScalar> operator *(Vector<TScalar> vector, TScalar scalar)
    {
        var result = new Vector<TScalar>(vector.Dimension);

        for (int i = 0; i < vector.Dimension; ++i)
        {
            result.Components[i] = vector.Components[i] * scalar;
        }
        return result;
    }

    public static Vector<TScalar> operator *(TScalar scalar, Vector<TScalar> vector)
    {
        var result = new Vector<TScalar>(vector.Dimension);

        for (int i = 0; i < vector.Dimension; ++i)
        {
            result.Components[i] = vector.Components[i] * scalar;
        }
        return result;
    }

    public static Vector<TScalar> operator /(Vector<TScalar> vector, TScalar scalar)
    {
        var result = new Vector<TScalar>(vector.Dimension);

        for (int i = 0; i < vector.Dimension; ++i)
        {
            result.Components[i] = vector.Components[i] / scalar;
        }
        return result;
    }

    public static Vector<TScalar> operator /(TScalar scalar, Vector<TScalar> vector)
    {
        var result = new Vector<TScalar>(vector.Dimension);

        for (int i = 0; i < vector.Dimension; ++i)
        {
            result.Components[i] = vector.Components[i] / scalar;
        }
        return result;
    }

    public static Vector<TScalar> operator +(Vector<TScalar> vector, TScalar scalar)
    {
        var result = new Vector<TScalar>(vector.Dimension);

        for (int i = 0; i < vector.Dimension; ++i)
        {
            result.Components[i] = vector.Components[i] + scalar;
        }
        return result;
    }

    public static Vector<TScalar> operator +(TScalar scalar, Vector<TScalar> vector)
    {
        var result = new Vector<TScalar>(vector.Dimension);

        for (int i = 0; i < vector.Dimension; ++i)
        {
            result.Components[i] = vector.Components[i] + scalar;
        }
        return result;
    }

    public static Vector<TScalar> operator -(Vector<TScalar> vector, TScalar scalar)
    {
        var result = new Vector<TScalar>(vector.Dimension);

        for (int i = 0; i < vector.Dimension; ++i)
        {
            result.Components[i] = vector.Components[i] - scalar;
        }
        return result;
    }

    public static Vector<TScalar> operator -(TScalar scalar, Vector<TScalar> vector)
    {
        var result = new Vector<TScalar>(vector.Dimension);

        for (int i = 0; i < vector.Dimension; ++i)
        {
            result.Components[i] = vector.Components[i] - scalar;
        }
        return result;
    }

    private static bool IsEqual(Vector<TScalar> left, Vector<TScalar> right)
    {
        if (left.Dimension != right.Dimension)
        {
            return false;
        }

        for (int i = 0; i < left.Dimension; ++i)
        {
            if (left.Components[i] != right.Components[i])
            {
                return false;
            }
        }
        return true;
    }

    public static bool operator ==(Vector<TScalar> left, Vector<TScalar> right)
    {
        return IsEqual(left, right);
    }

    public static bool operator !=(Vector<TScalar> left, Vector<TScalar> right)
    {
        return !IsEqual(left, right);
    }

    public TScalar SquaredNorm()
    {
        return Dot(this);
    }

    public TScalar AbsSum()
    {
        TScalar result = default;

        for (int i = 0; i < Dimension; ++i)
        {
            var nextValue = Components[i];

            if (result > TScalar.MaxValue - nextValue)
            {
                return TScalar.MaxValue;
            }
            result += nextValue;
        }
        return result;
    }

    public TScalar Dot(Vector<TScalar> otherVector)
    {
        if (Dimension != otherVector.Dimension)
        {
            throw new InvalidOperationException("Cannot calculate dot product of vectors of different dimensions.");
        }
        TScalar result = default;

        for (int i = 0; i < Dimension; ++i)
        {
            result += Components[i] * otherVector.Components[i];
        }
        return result;
    }

    public static Vector<TScalar> CrossProduct(Vector<TScalar> left, Vector<TScalar> right)
    {
        if (left.Dimension != 3 || right.Dimension != 3)
        {
            throw new InvalidOperationException("Cross product is only defined for 3D vectors.");
        }
        var result = new Vector<TScalar>(3)
        {
            Components =
            [
                (left.Components[1] * right.Components[2]) - (left.Components[2] * right.Components[1]),
                (left.Components[2] * right.Components[0]) - (left.Components[0] * right.Components[2]),
                (left.Components[0] * right.Components[1]) - (left.Components[1] * right.Components[0])
            ]
        };
        return result;
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector<TScalar> vector ? IsEqual(this, vector) : false;
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}

internal class Vector2<TScalar> : Vector<TScalar>
    where TScalar : struct,
        IComparisonOperators<TScalar, TScalar, bool>,
        IComparable,
        IEqualityOperators<TScalar, TScalar, bool>,
        IAdditionOperators<TScalar, TScalar, TScalar>,
        ISubtractionOperators<TScalar, TScalar, TScalar>,
        IDivisionOperators<TScalar, TScalar, TScalar>,
        IMultiplyOperators<TScalar, TScalar, TScalar>,
        IDecrementOperators<TScalar>,
        IBitwiseOperators<TScalar, TScalar, TScalar>,
        IMinMaxValue<TScalar>
{
    public Vector2(TScalar c0, TScalar c1) : base(c0, c1) { }
    public Vector2(params TScalar[] c) : this(c[0], c[1]) { }
    public Vector2(Vector<TScalar> vector) : this(vector.Components[0], vector.Components[1]) { }
}

internal class Vector3<TScalar> : Vector<TScalar>
    where TScalar : struct,
        IComparisonOperators<TScalar, TScalar, bool>,
        IComparable,
        IEqualityOperators<TScalar, TScalar, bool>,
        IAdditionOperators<TScalar, TScalar, TScalar>,
        ISubtractionOperators<TScalar, TScalar, TScalar>,
        IDivisionOperators<TScalar, TScalar, TScalar>,
        IMultiplyOperators<TScalar, TScalar, TScalar>,
        IDecrementOperators<TScalar>,
        IBitwiseOperators<TScalar, TScalar, TScalar>,
        IMinMaxValue<TScalar>
{
    public Vector3(TScalar c0, TScalar c1, TScalar c2) : base(c0, c1, c2) { }
    public Vector3(params TScalar[] c) : this(c[0], c[1], c[2]) { }
    public Vector3(Vector<TScalar> vector) : this(vector.Components[0], vector.Components[1], vector.Components[2]) { }
}
