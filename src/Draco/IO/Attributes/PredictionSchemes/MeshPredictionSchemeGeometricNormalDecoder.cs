using System.Numerics;
using Draco.IO.BitCoders;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class MeshPredictionSchemeGeometricNormalDecoder<TDataType, TTransform>(PointAttribute attribute, TTransform transform, MeshPredictionSchemeData meshData) : MeshPredictionSchemeDecoder<TDataType, TTransform>(attribute, transform, meshData)
    where TDataType : struct,
        IComparisonOperators<TDataType, TDataType, bool>,
        IComparable,
        IEqualityOperators<TDataType, TDataType, bool>,
        IAdditionOperators<TDataType, TDataType, TDataType>,
        ISubtractionOperators<TDataType, TDataType, TDataType>,
        IDivisionOperators<TDataType, TDataType, TDataType>,
        IMultiplyOperators<TDataType, TDataType, TDataType>,
        IDecrementOperators<TDataType>,
        IBitwiseOperators<TDataType, TDataType, TDataType>,
        IMinMaxValue<TDataType>
    where TTransform : PredictionSchemeDecodingTransform<TDataType>
{
    public override PredictionSchemeMethod Method { get => PredictionSchemeMethod.GeometricNormal; }
    private readonly MeshPredictionSchemeGeometricNormalPredictorArea _predictor = new(meshData);
    private readonly OctahedronToolBox _octahedronToolBox = new();
    private readonly RAnsBitDecoder _flipNormalBitDecoder = new();

    public override int ParentAttributesCount { get => 1; set { } }

    public bool IsInitialized()
    {
        return _predictor.IsInitialized() && MeshData.IsInitialized() && _octahedronToolBox.IsInitialized();
    }

    public override GeometryAttributeType GetParentAttributeType(int i)
    {
        return GeometryAttributeType.Position;
    }

    public void SetParentAttribute(PointAttribute attribute)
    {
        Assertions.ThrowIf(attribute.AttributeType != GeometryAttributeType.Position);
        Assertions.ThrowIf(attribute.NumComponents != 3);
        _predictor.PositionAttribute = attribute;
    }

    public void SetQuantizationBits(byte q)
    {
        _octahedronToolBox.SetQuantizationBits(q);
    }

    public override TDataType[] ComputeOriginalValues(TDataType[] correctedData, int _, int numComponents, List<uint> entryToPointMap)
    {
        SetQuantizationBits((byte)Transform.QuantizationBits);
        _predictor.EntryToPointIdMap = entryToPointMap;
        TDataType[] data = new TDataType[MeshData.DataToCornerMap!.Count * 2];

        for (int dataId = 0; dataId < MeshData.DataToCornerMap!.Count; ++dataId)
        {
            var cornerId = MeshData.DataToCornerMap[dataId];
            var predictedNormal3D = _predictor.ComputePredictedValue(cornerId);
            var predictedNormal3DData = predictedNormal3D.Components;
            _octahedronToolBox.CanonicalizeIntegerVector(ref predictedNormal3DData);
            predictedNormal3D = new Core.Vector<int>(predictedNormal3DData);

            if (_flipNormalBitDecoder.DecodeNextBit() != 0)
            {
                predictedNormal3D = 0 - predictedNormal3D;
            }
            var (s, t) = _octahedronToolBox.IntegerVectorToQuantizedOctahedralCoords(predictedNormal3D.Components);
            TDataType[] predictedNormalOctahedral = [(TDataType)Convert.ChangeType(s, typeof(TDataType)), (TDataType)Convert.ChangeType(t, typeof(TDataType))];
            var dataOffset = dataId * 2;
            data.SetSubArray(Transform.ComputeOriginalValue(predictedNormalOctahedral, correctedData.GetSubArray(dataOffset)), dataOffset);
        }
        _flipNormalBitDecoder.EndDecoding();
        return data;
    }

    public override void DecodePredictionData(DecoderBuffer decoderBuffer)
    {
        Transform.DecodeTransformData(decoderBuffer);

        if (decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 2))
        {
            var predictionMode = decoderBuffer.ReadByte();
            Assertions.ThrowIf(predictionMode > (byte)NormalPredictionMode.TriangleArea, "Invalid prediction mode.");
            _predictor.Mode = (NormalPredictionMode)predictionMode;
        }
        _flipNormalBitDecoder.StartDecoding(decoderBuffer);
    }
}
