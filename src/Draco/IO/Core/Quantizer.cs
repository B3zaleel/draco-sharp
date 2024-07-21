namespace Draco.IO.Core;

public class Quantizer
{
    private readonly float _inverseDelta = 1.0f;

    public Quantizer(float delta)
    {
        _inverseDelta = 1.0f / delta;
    }

    public Quantizer(float range, int maxQuantizedValue)
    {
        _inverseDelta = maxQuantizedValue / range;
    }

    public int QuantizeFloat(float value)
    {
        value += _inverseDelta;
        return (int)Math.Floor(value + 0.5f);
    }
}
