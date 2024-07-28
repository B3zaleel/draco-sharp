using Draco.IO.Attributes.PredictionSchemes;
using Draco.IO.Entropy;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes;

internal class SequentialIntegerAttributeDecoder : SequentialAttributeDecoder
{
    private IPredictionSchemeDecoder<int>? _predictionScheme;
    public int ValueComponentsCount { get => Attribute!.NumComponents; }

    public SequentialIntegerAttributeDecoder() { }

    public override void TransformAttributeToOriginalFormat(List<uint> pointIds)
    {
        if (ConnectivityDecoder != null && ConnectivityDecoder.BitStreamVersion < Constants.BitStreamVersion(2, 0))
        {
            return;
        }
        StoreValues((uint)pointIds.Count);
    }

    protected override void DecodeValues(DecoderBuffer decoderBuffer, List<uint> pointIds)
    {
        var predictionSchemeMethod = decoderBuffer.ReadSByte();
        Assertions.ThrowIf(predictionSchemeMethod < (sbyte)PredictionSchemeMethod.None || predictionSchemeMethod >= (sbyte)PredictionSchemeMethod.Count, "Invalid prediction scheme method.");

        if (predictionSchemeMethod != (sbyte)PredictionSchemeMethod.None)
        {
            var predictionTransformType = decoderBuffer.ReadSByte();
            Assertions.ThrowIf(predictionTransformType < (sbyte)PredictionSchemeTransformType.None || predictionTransformType >= (sbyte)PredictionSchemeTransformType.Count, "Invalid prediction transform type.");
            _predictionScheme = CreatePredictionScheme((PredictionSchemeMethod)predictionSchemeMethod, (PredictionSchemeTransformType)predictionTransformType);
        }
        if (_predictionScheme != null)
        {
            InitPredictionScheme(decoderBuffer, _predictionScheme);
        }
        DecodeIntegerValues(decoderBuffer, pointIds);

        if (decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 0))
        {
            StoreValues((uint)pointIds.Count);
        }
    }

    protected IPredictionSchemeDecoder<int>? CreatePredictionScheme(PredictionSchemeMethod method, PredictionSchemeTransformType transformType)
    {
        return transformType == PredictionSchemeTransformType.Wrap
            ? (IPredictionSchemeDecoder<int>?)PredictionSchemeDecoderFactory.CreatePredictionSchemeForDecoder<int, PredictionSchemeWrapDecodingTransform<int>>(method, AttributeId, ConnectivityDecoder!, new())
            : null;
    }

    protected void DecodeIntegerValues(DecoderBuffer decoderBuffer, List<uint> pointIds)
    {
        var numComponents = Attribute!.NumComponents;
        Assertions.ThrowIf(numComponents <= 0);
        var numEntries = pointIds.Count;
        var numValues = numEntries * numComponents;
        PreparePortableAttribute(numEntries, numComponents);
        uint[] portableAttributeData = [];
        var compressed = decoderBuffer.ReadByte();

        if (compressed > 0)
        {
            SymbolDecoding.DecodeSymbols(decoderBuffer, (uint)numValues, numComponents, out portableAttributeData);
            PortableAttribute!.Buffer = StreamExtensions.Create(portableAttributeData);
        }
        else
        {
            var numBytes = decoderBuffer.ReadByte();

            if (numBytes == Constants.DataTypeLength(DataType.Int32))
            {
                PortableAttribute!.Buffer = StreamExtensions.Create(decoderBuffer.ReadBytes(sizeof(int) * numValues));
            }
            else
            {
                for (int i = 0; i < numValues; ++i)
                {
                    decoderBuffer.ReadBytes(numBytes);
                }
            }
        }
        int[] portableAttributeDataAsInt = [];
        if (numValues > 0 && (_predictionScheme == null || _predictionScheme.AreCorrectionsPositive()))
        {
            portableAttributeDataAsInt = BitUtilities.ConvertSymbolsToSignedInts(portableAttributeData);
        }
        if (_predictionScheme != null)
        {
            _predictionScheme.DecodePredictionData(decoderBuffer);

            if (numValues > 0)
            {
                var originalData = _predictionScheme.ComputeOriginalValues(portableAttributeDataAsInt, numValues, numComponents, pointIds);
            }
        }
    }

    protected void StoreValues(uint numValues)
    {
        switch (Attribute!.DataType)
        {
            case DataType.UInt8:
                {
                    StoreTypedValues<byte>(numValues);
                    break;
                }
            case DataType.Int8:
                {
                    StoreTypedValues<sbyte>(numValues);
                    break;
                }
            case DataType.UInt16:
                {
                    StoreTypedValues<ushort>(numValues);
                    break;
                }
            case DataType.Int16:
                {
                    StoreTypedValues<short>(numValues);
                    break;
                }
            case DataType.UInt32:
                {
                    StoreTypedValues<uint>(numValues);
                    break;
                }
            case DataType.Int32:
                {
                    StoreTypedValues<int>(numValues);
                    break;
                }
            default:
                throw new NotImplementedException();
        };
    }

    private void StoreTypedValues<T>(uint numValues)
    {
        var numComponents = Attribute!.NumComponents;
        var entrySize = sizeof(int) * numComponents;
        var attributeValue = new T[numComponents];
        // int valId = 0;

        for (uint i = 0; i < numValues; ++i)
        {
            for (int c = 0; c < numComponents; ++c)
            {
                attributeValue[c] = PortableAttribute!.Buffer!.ReadDatum<T>();
            }
            var ms = new MemoryStream();
            Attribute.Buffer!.WriteData(attributeValue);
        }
    }

    protected void PreparePortableAttribute(int numEntries, int numComponents)
    {
        var geometryAttribute = new GeometryAttribute(Attribute!.AttributeType, null, (byte)numComponents, DataType.Int32, false, numComponents * Constants.DataTypeLength(DataType.Int32), 0);
        PortableAttribute = new PointAttribute(geometryAttribute);
        PortableAttribute.SetIdentityMapping();
        PortableAttribute.Reset(numEntries);
        PortableAttribute.UniqueId = Attribute.UniqueId;
    }
}
