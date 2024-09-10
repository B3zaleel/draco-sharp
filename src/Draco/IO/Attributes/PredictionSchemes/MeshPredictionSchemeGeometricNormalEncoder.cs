using System.Numerics;
using Draco.IO.BitCoders;
using Draco.IO.Enums;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class MeshPredictionSchemeGeometricNormalEncoder<TDataType, TTransform>(PointAttribute attribute, TTransform transform, MeshPredictionSchemeData meshData) : MeshPredictionSchemeEncoder<TDataType, TTransform>(attribute, transform, meshData)
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
    where TTransform : IPredictionSchemeEncodingTransform<TDataType, TDataType>
{
    private readonly MeshPredictionSchemeGeometricNormalPredictorArea _predictor = new(meshData);
    private readonly OctahedronToolBox _octahedronToolBox = new();
    private readonly RAnsBitEncoder _flipNormalBitEncoder = new();
    public override PredictionSchemeMethod Method { get => PredictionSchemeMethod.GeometricNormal; }

    public override int ParentAttributesCount { get => 1; set { } }

    public bool IsInitialized()
    {
        return _predictor.IsInitialized() && MeshData.IsInitialized();
    }

    public override GeometryAttributeType GetParentAttributeType(int i)
    {
        return GeometryAttributeType.Position;
    }

    public void SetQuantizationBits(byte q)
    {
        _octahedronToolBox.SetQuantizationBits(q);
    }

    public override TDataType[] ComputeCorrectionValues(TDataType[] data, int size, int numComponents, List<uint> entryToPointMap)
    {
        SetQuantizationBits((byte)Transform.QuantizationBits);
        _predictor.EntryToPointIdMap = entryToPointMap;
        Assertions.ThrowIfNot(IsInitialized(), "Mesh prediction scheme is not initialized.");
        Assertions.ThrowIfNot(numComponents == 2, "Expecting `data` in octahedral coordinates, i.e., portable attribute.");
        var correctionValues = new TDataType[numComponents];
        _flipNormalBitEncoder.StartEncoding();
        var cornerMapSize = MeshData.DataToCornerMap!.Count;
        var predictionNormal3D = new Core.Vector<int>(new int[3]);
        var positivePredictionNormalOctahedron = new TDataType[2];
        var negativePredictionNormalOctahedron = new TDataType[2];
        var positiveCorrection = new Core.Vector<int>(new int[2]);
        var negativeCorrection = new Core.Vector<int>(new int[2]);

        for (int dataId = 0; dataId < cornerMapSize; ++dataId)
        {
            var cornerId = MeshData.DataToCornerMap[dataId];
            var predictionNormal3DComponents = _predictor.ComputePredictedValue(cornerId).Components!;

            _octahedronToolBox.CanonicalizeIntegerVector(ref predictionNormal3DComponents);
            predictionNormal3D = new Core.Vector<int>(predictionNormal3DComponents);

            var positivePredictionNormalOctahedralCoordinates = _octahedronToolBox.IntegerVectorToQuantizedOctahedralCoords(predictionNormal3D.Components);
            positivePredictionNormalOctahedron[0] = Constants.ConstCast<int, TDataType>(positivePredictionNormalOctahedralCoordinates.S);
            positivePredictionNormalOctahedron[1] = Constants.ConstCast<int, TDataType>(positivePredictionNormalOctahedralCoordinates.T);
            predictionNormal3D = 0 - predictionNormal3D;
            var negativePredictionNormalOctahedralCoordinates = _octahedronToolBox.IntegerVectorToQuantizedOctahedralCoords(predictionNormal3D.Components);
            negativePredictionNormalOctahedron[0] = Constants.ConstCast<int, TDataType>(negativePredictionNormalOctahedralCoordinates.S);
            negativePredictionNormalOctahedron[1] = Constants.ConstCast<int, TDataType>(negativePredictionNormalOctahedralCoordinates.T);

            int dataOffset = dataId * numComponents; // numComponents == 2
            var positiveCorrectionAsTDataType = Transform.ComputeCorrectionValue(data.GetSubArray(dataOffset, numComponents), positivePredictionNormalOctahedron);
            positiveCorrection[0] = Constants.ConstCast<TDataType, int>(positiveCorrectionAsTDataType[0]);
            positiveCorrection[0] = Constants.ConstCast<TDataType, int>(positiveCorrectionAsTDataType[1]);
            var negativeCorrectionAsTDataType = Transform.ComputeCorrectionValue(data.GetSubArray(dataOffset, numComponents), negativePredictionNormalOctahedron);
            negativeCorrection[0] = Constants.ConstCast<TDataType, int>(negativeCorrectionAsTDataType[0]);
            negativeCorrection[1] = Constants.ConstCast<TDataType, int>(negativeCorrectionAsTDataType[1]);

            positiveCorrection[0] = _octahedronToolBox.ModMax(positiveCorrection[0]);
            positiveCorrection[1] = _octahedronToolBox.ModMax(positiveCorrection[1]);
            negativeCorrection[0] = _octahedronToolBox.ModMax(negativeCorrection[0]);
            negativeCorrection[1] = _octahedronToolBox.ModMax(negativeCorrection[1]);

            if (positiveCorrection.AbsSum() < negativeCorrection.AbsSum())
            {
                _flipNormalBitEncoder.EncodeBit(false);
                correctionValues[dataOffset] = Constants.ConstCast<int, TDataType>(_octahedronToolBox.MakePositive(positiveCorrection[0]));
                correctionValues[dataOffset + 1] = Constants.ConstCast<int, TDataType>(_octahedronToolBox.MakePositive(positiveCorrection[1]));
            }
            else
            {
                _flipNormalBitEncoder.EncodeBit(true);
                correctionValues[dataOffset] = Constants.ConstCast<int, TDataType>(_octahedronToolBox.MakePositive(negativeCorrection[0]));
                correctionValues[dataOffset + 1] = Constants.ConstCast<int, TDataType>(_octahedronToolBox.MakePositive(negativeCorrection[1]));
            }
        }
        return correctionValues;
    }

    public override void EncodePredictionData(EncoderBuffer encoderBuffer)
    {
        Transform.EncodeTransformData(encoderBuffer);
        _flipNormalBitEncoder.EndEncoding(encoderBuffer);
    }
}
