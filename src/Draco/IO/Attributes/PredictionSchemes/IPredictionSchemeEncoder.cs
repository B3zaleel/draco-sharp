namespace Draco.IO.Attributes.PredictionSchemes;

internal interface IPredictionSchemeEncoder<TDataType> : IPredictionSchemeEncoder<TDataType, TDataType>
{
}

internal interface IPredictionSchemeEncoder<TDataType, TCorrectedType> : IPredictionSchemeEncoder
{
    public TCorrectedType[] ComputeCorrectionValues(TDataType[] data, int size, int numComponents, List<uint> entryToPointMap);
}

internal interface IPredictionSchemeEncoder : IPredictionScheme
{
    public void EncodePredictionData(EncoderBuffer encoderBuffer);
}
