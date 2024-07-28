using Draco.IO.Extensions;

namespace Draco.IO.Attributes;

internal class OctahedronToolBox
{
    public int QuantizationBits { get; private set; } = -1;
    public int MaxQuantizedValue { get; private set; } = -1;
    public int MaxValue { get; private set; } = -1;
    private float _dequantizationScale = 1.0f;
    public int CenterValue { get; private set; } = -1;

    public void SetQuantizationBits(byte value)
    {
        Assertions.ThrowIf(value < 2 || value > 30);
        QuantizationBits = value;
        MaxQuantizedValue = (1 << QuantizationBits) - 1;
        MaxValue = MaxQuantizedValue - 1;
        _dequantizationScale = 2.0f / MaxValue;
        CenterValue = MaxValue / 2;
    }

    public bool IsInitialized()
    {
        return QuantizationBits != -1;
    }

    public (int S, int T) CanonicalizeOctahedralCoords(int s, int t)
    {
        if ((s == 0 && t == 0) || (s == 0 && t == MaxValue) || (s == MaxValue && t == 0))
        {
            return (MaxValue, MaxValue);
        }
        else if (s == 0 && t > CenterValue)
        {
            return (s, CenterValue - (t - CenterValue));
        }
        else if (s == MaxValue && t < CenterValue)
        {
            return (s, CenterValue + (CenterValue - t));
        }
        else if (t == MaxValue && s < CenterValue)
        {
            return (CenterValue + (CenterValue - s), t);
        }
        else if (t == 0 && s > CenterValue)
        {
            return (CenterValue - (s - CenterValue), t);
        }
        else
        {
            return (s, t);
        }
    }

    /// <summary>
    /// Converts an integer vector to octahedral coordinates.
    /// </summary>
    /// <param name="vector">A 3D integer vector whose absolute sum must equal the center value.</param>
    /// <returns></returns>
    public (int S, int T) IntegerVectorToQuantizedOctahedralCoords(int[] vector)
    {
        Assertions.ThrowIfNot(Math.Abs(vector[0] + vector[1] + vector[2]) == CenterValue);
        int s, t;

        if (vector[0] >= 0)
        {
            s = vector[1] + CenterValue;
            t = vector[2] + CenterValue;
        }
        else
        {
            s = vector[1] < 0 ? Math.Abs(vector[2]) : MaxValue - Math.Abs(vector[2]);
            t = vector[2] < 0 ? Math.Abs(vector[1]) : MaxValue - Math.Abs(vector[1]);
        }
        return CanonicalizeOctahedralCoords(s, t);
    }

    public (int S, int T) FloatVectorToQuantizedOctahedralCoords(double[] vector)
    {
        var absSum = Math.Abs(vector[0]) + Math.Abs(vector[1]) + Math.Abs(vector[2]);
        var scaledVector = new double[3];

        if (absSum > 1E-6)
        {
            var scale = 1.0 / absSum;
            scaledVector[0] = vector[0] * scale;
            scaledVector[1] = vector[1] * scale;
            scaledVector[2] = vector[2] * scale;
        }
        else
        {
            scaledVector[0] = 1.0;
            scaledVector[1] = 0;
            scaledVector[2] = 0;
        }
        var intVector = new int[3];
        intVector[0] = (int)Math.Floor(scaledVector[0] * CenterValue + 0.5);
        intVector[1] = (int)Math.Floor(scaledVector[1] * CenterValue + 0.5);
        intVector[2] = CenterValue - Math.Abs(intVector[0]) - Math.Abs(intVector[1]);

        if (intVector[2] < 0)
        {
            if (intVector[1] > 0)
            {
                intVector[1] += intVector[2];
            }
            else
            {
                intVector[1] -= intVector[2];
            }
            intVector[2] = 0;
        }
        if (scaledVector[2] < 0)
        {
            intVector[2] *= -1;
        }
        return IntegerVectorToQuantizedOctahedralCoords(intVector);
    }

    public void CanonicalizeIntegerVector(ref int[] vector)
    {
        var absSum = Math.Abs(vector[0]) + Math.Abs(vector[1]) + Math.Abs(vector[2]);

        if (absSum == 0)
        {
            vector[0] = CenterValue;
        }
        else
        {
            vector[0] = vector[0] * CenterValue / absSum;
            vector[1] = vector[1] * CenterValue / absSum;
            vector[2] = vector[2] >= 0
                ? CenterValue - Math.Abs(vector[0]) - Math.Abs(vector[1])
                : -(CenterValue - Math.Abs(vector[0]) - Math.Abs(vector[1]));
        }
    }

    public float[] QuantizedOctahedralCoordsToUnitVector(int s, int t)
    {
        return OctahedralCoordsToUnitVector(s * _dequantizationScale - 1.0f, t * _dequantizationScale - 1.0f);
    }

    public bool IsInDiamond(int s, int t)
    {
        Assertions.ThrowIfNot(s <= CenterValue && t <= CenterValue);
        Assertions.ThrowIfNot(s >= -CenterValue && t >= -CenterValue);
        var st = (uint)Math.Abs(s) + (uint)Math.Abs(t);
        return st <= CenterValue;
    }

    public void InvertDiamond(ref int s, ref int t)
    {
        Assertions.ThrowIfNot(s <= CenterValue && t <= CenterValue);
        Assertions.ThrowIfNot(s >= -CenterValue && t >= -CenterValue);
        int signS = 0;
        int signT = 0;

        if (s >= 0 && t >= 0)
        {
            signS = 1;
            signT = 1;
        }
        else if (s <= 0 && t <= 0)
        {
            signS = -1;
            signT = -1;
        }
        else
        {
            signS = (s > 0) ? 1 : -1;
            signT = (t > 0) ? 1 : -1;
        }
        var cornerPointS = signS * CenterValue;
        var cornerPointT = signT * CenterValue;
        var us = s + s - cornerPointS;
        var ut = t + t - cornerPointT;
        var temp = us;

        if (signS * signT >= 0)
        {
            us = -ut;
            ut = -temp;
        }
        else
        {
            us = ut;
            ut = temp;
        }
        us += cornerPointS;
        ut += cornerPointT;
        s = us / 2;
        t = ut / 2;
    }

    public void InvertDirection(ref int s, ref int t)
    {
        Assertions.ThrowIfNot(s <= CenterValue && t <= CenterValue);
        Assertions.ThrowIfNot(s >= -CenterValue && t >= -CenterValue);
        s *= -1;
        t *= -1;
        InvertDiamond(ref s, ref t);
    }

    public int ModMax(int x)
    {
        if (x > CenterValue)
        {
            return x - MaxQuantizedValue;
        }
        return x < -CenterValue ? x + MaxQuantizedValue : x;
    }

    public int MakePositive(int x)
    {
        Assertions.ThrowIfNot(x <= CenterValue + 2);
        return x < 0 ? x + MaxQuantizedValue : x;
    }

    private float[] OctahedralCoordsToUnitVector(float sScaled, float tScaled)
    {
        var y = sScaled;
        var z = tScaled;
        var x = 1.0f - Math.Abs(y) - Math.Abs(z);
        var xOffset = -x < 0 ? 0 : -x;
        y += y < 0 ? xOffset : -xOffset;
        z += z < 0 ? xOffset : -xOffset;
        var normSquared = x * x + y * y + z + z;

        if (normSquared < 1E-6)
        {
            return [0, 0, 0];
        }
        else
        {
            var d = 1.0f / Math.Sqrt(normSquared);
            return [(float)(x * d), (float)(y * d), (float)(z * d)];
        }
    }
}
