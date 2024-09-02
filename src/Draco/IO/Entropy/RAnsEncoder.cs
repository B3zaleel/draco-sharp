namespace Draco.IO.Entropy;

internal class RAnsEncoder : AnsEncoder
{
    private readonly int _rAnsPrecisionBits;
    private readonly int _rAnsPrecision;

    public RAnsEncoder(int rAnsPrecisionBits)
    {
        _rAnsPrecisionBits = rAnsPrecisionBits;
        _rAnsPrecision = 1 << rAnsPrecisionBits;
        LRAnsBase = _rAnsPrecision * 4;
    }

    public override void WriteInit(byte[] buffer)
    {
        Buffer = buffer;
        BufferOffset = 0;
        State = (uint)LRAnsBase;
    }

    public void Write(RAnsSymbol symbol)
    {
        while (State >= LRAnsBase / _rAnsPrecision * Constants.AnsIOBase * symbol.Probability)
        {
            Buffer[BufferOffset++] = (byte)(State % Constants.AnsIOBase);
            State /= Constants.AnsIOBase;
        }
        State = (uint)((State / symbol.Probability) * _rAnsPrecision + State % symbol.Probability + symbol.CumulativeProbability);
    }
}
