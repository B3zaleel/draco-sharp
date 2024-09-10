using Draco.IO.Attributes.PredictionSchemes;
using Draco.IO.Entropy;
using Draco.IO.Enums;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes;

internal class SequentialIntegerAttributeEncoder : SequentialAttributeEncoder
{
    private readonly IPredictionSchemeEncoder<int>? _predictionScheme;
    public override byte UniqueId { get => (byte)SequentialAttributeEncoderType.Integer; }

    public SequentialIntegerAttributeEncoder(ConnectivityEncoder connectivityEncoder, int attributeId) : base(connectivityEncoder, attributeId)
    {
        var method = PredictionSchemeEncoderFactory.GetPredictionMethod(connectivityEncoder.Config, attributeId);
        _predictionScheme = CreatePredictionScheme(method);
        InitPredictionScheme(_predictionScheme!);
    }

    protected virtual IPredictionSchemeEncoder<int>? CreatePredictionScheme(PredictionSchemeMethod method)
    {
        return PredictionSchemeEncoderFactory.CreatePredictionScheme<int, IPredictionSchemeEncodingTransform<int, int>>(method, AttributeId, ConnectivityEncoder!, new PredictionSchemeWrapEncodingTransform<int>());
    }

    public override void TransformAttributeToPortableFormat(List<uint> pointIds)
    {
        if (ConnectivityEncoder == null)
        {
            PrepareValues(pointIds, 0);
        }
        else
        {
            PrepareValues(pointIds, ConnectivityEncoder.PointCloud!.PointsCount);
        }
        if (IsParentEncoder)
        {
            var originalAttribute = Attribute;
            var portableAttribute = PortableAttribute;
            var valueToValueMap = new uint[originalAttribute!.UniqueEntriesCount];
            for (int i = 0; i < pointIds.Count; ++i)
            {
                valueToValueMap[originalAttribute.MappedIndex(pointIds[i])] = (uint)i;
            }
            if (portableAttribute!.IsMappingIdentity)
            {
                portableAttribute.SetExplicitMapping(ConnectivityEncoder!.PointCloud!.PointsCount);
            }
            for (uint i = 0; i < ConnectivityEncoder!.PointCloud!.PointsCount; ++i)
            {
                portableAttribute.SetPointMapEntry(i, valueToValueMap[originalAttribute.MappedIndex(i)]);
            }
        }
    }

    protected override void EncodeValues(EncoderBuffer encoderBuffer, List<uint> pointIds)
    {
        if (Attribute!.UniqueEntriesCount == 0)
        {
            return;
        }
        var predictionSchemeMethod = PredictionSchemeMethod.None;
        if (_predictionScheme != null)
        {
            predictionSchemeMethod = _predictionScheme.Method;
        }
        encoderBuffer.WriteSByte((sbyte)predictionSchemeMethod);
        if (_predictionScheme != null)
        {
            encoderBuffer.WriteSByte((sbyte)_predictionScheme.TransformType);
        }
        int numValues = (int)(PortableAttribute!.NumComponents * PortableAttribute.UniqueEntriesCount);
        var portableAttributeData = PortableAttribute.Buffer!.Read<int>(0, (int)PortableAttribute.Buffer.DataSize / Constants.DataTypeLength(DataType.Int32));
        int[] encodedData = [];

        if (_predictionScheme != null)
        {
            encodedData = _predictionScheme.ComputeCorrectionValues(portableAttributeData, numValues, PortableAttribute.NumComponents, pointIds);
        }
        if (_predictionScheme == null || !_predictionScheme.AreCorrectionsPositive())
        {
            var input = _predictionScheme == null ? portableAttributeData : encodedData;
            encodedData = Constants.ReinterpretCast<uint, int>(BitUtilities.ConvertSignedIntsToSymbols(input));
        }
        if (ConnectivityEncoder == null || ConnectivityEncoder.Config.GetOption(ConfigOptionName.UseBuiltInAttributeCompression, true))
        {
            encoderBuffer.WriteByte(1);
            var symbolEncodingConfig = new Config();
            if (ConnectivityEncoder != null)
            {
                symbolEncodingConfig.SetSymbolEncodingCompressionLevel(10 - ConnectivityEncoder.Config.Speed);
            }
            SymbolEncoding.EncodeSymbols(encoderBuffer, symbolEncodingConfig, Constants.ReinterpretCast<int, uint>(encodedData).ToList(), PortableAttribute.NumComponents);
        }
        else
        {
            uint maskedValue = 0;
            for (uint i = 0; i < numValues; ++i)
            {
                maskedValue |= (uint)encodedData[i];
            }
            int valueMSBPosition = 0;
            if (maskedValue != 0)
            {
                valueMSBPosition = BitUtilities.MostSignificantBit(maskedValue);
            }
            int numBytes = 1 + valueMSBPosition / 8;
            encoderBuffer.WriteByte(0);
            encoderBuffer.WriteByte((byte)numBytes);
            if (numBytes == Constants.DataTypeLength(DataType.Int32))
            {
                for (uint i = 0; i < numValues; ++i)
                {
                    encoderBuffer.WriteInt32(encodedData[i]);
                }
            }
            else
            {
                for (uint i = 0; i < numValues; ++i)
                {
                    encoderBuffer.WriteBytes(BitConverter.GetBytes(encodedData[i]).GetSubArray(0, numBytes));
                }
            }
        }
        if (_predictionScheme != null)
        {
            _predictionScheme.EncodePredictionData(encoderBuffer);
        }
    }

    protected virtual void PrepareValues(List<uint> pointIds, int numPoints)
    {
        PreparePortableAttribute(pointIds.Count, Attribute!.NumComponents, numPoints);
        int dstIndex = 0;

        foreach (var pointId in pointIds)
        {
            PortableAttribute!.Buffer!.Write(Attribute.ConvertValue<int>(Attribute.MappedIndex(pointId)), dstIndex);
            dstIndex += Attribute.NumComponents;
        }
    }

    protected void PreparePortableAttribute(int numEntries, int numComponents, int numPoints)
    {
        var portableAttribute = new PointAttribute(new GeometryAttribute(Attribute!.AttributeType, null, (byte)numComponents, DataType.Int32, false, numComponents * Constants.DataTypeLength(DataType.Int32), 0));
        portableAttribute.Reset(numEntries);
        PortableAttribute = portableAttribute;
        if (numPoints != 0)
        {
            portableAttribute.SetExplicitMapping(numPoints);
        }
    }
}
