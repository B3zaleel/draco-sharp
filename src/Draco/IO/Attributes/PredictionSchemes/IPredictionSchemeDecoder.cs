namespace Draco.IO.Attributes.PredictionSchemes;

internal interface IPredictionSchemeDecoder<TDataType> : IPredictionSchemeDecoder<TDataType, TDataType>
{
}

internal interface IPredictionSchemeDecoder<TDataType, TCorrectedType> : IPredictionSchemeDecoder
{
    public TCorrectedType[] ComputeOriginalValues(TDataType[] data, int size, int numComponents, List<uint> entryToPointMap);
}

internal interface IPredictionSchemeDecoder : IPredictionScheme
{
    public void DecodePredictionData(DecoderBuffer decoderBuffer);
}
