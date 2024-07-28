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
    public TScalar this[int index] { get => Components[index]; }

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
}
