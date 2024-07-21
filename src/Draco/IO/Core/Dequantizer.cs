using Draco.IO.Extensions;

namespace Draco.IO.Core;

public class Dequantizer
{
    private readonly float _delta;

    public Dequantizer(float delta)
    {
        _delta = delta;
    }

    public Dequantizer(float range, int maxQuantizedValue)
    {
        Assertions.ThrowIf(maxQuantizedValue <= 0);
        _delta = range / maxQuantizedValue;
    }

    public float DequantizeFloat(int value)
    {
        return value * _delta;
    }
}
