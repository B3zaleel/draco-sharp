using System.Numerics;
using Draco.IO.BitCoders;
using Draco.IO.Enums;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class MeshPredictionSchemeTexCoordsPortableEncoder<TDataType, TTransform>(PointAttribute attribute, TTransform transform, MeshPredictionSchemeData meshData) : MeshPredictionSchemeEncoder<TDataType, TTransform>(attribute, transform, meshData)
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
    private readonly MeshPredictionSchemeTexCoordsPortablePredictor<TDataType> _predictor = new(meshData, false);
    public override PredictionSchemeMethod Method { get => PredictionSchemeMethod.TexCoordsPortable; }
    public override int ParentAttributesCount { get => 1; set { } }
    public override GeometryAttributeType ParentAttributeType
    {
        get => GeometryAttributeType.Position;
    }
    public override PointAttribute? ParentAttribute
    {
        get => _predictor.PositionAttribute;
        set
        {
            Assertions.ThrowIf(value == null || value!.AttributeType != GeometryAttributeType.Position, "Invalid attribute type.");
            Assertions.ThrowIf(value!.NumComponents != 3, "Currently works only for 3 component positions.");
            _predictor.PositionAttribute = value;
        }
    }

    public bool IsInitialized()
    {
        return _predictor.IsInitialized() && MeshData.IsInitialized();
    }

    public override GeometryAttributeType GetParentAttributeType(int i)
    {
        return GeometryAttributeType.Position;
    }

    public override TDataType[] ComputeCorrectionValues(TDataType[] data, int size, int numComponents, List<uint> entryToPointMap)
    {
        _predictor.EntryToPointMap = entryToPointMap;
        Transform.Init(data, size, numComponents);
        var correctionValues = new TDataType[size * numComponents];

        for (int p = MeshData.DataToCornerMap!.Count - 1; p >= 0; --p)
        {
            var cornerId = MeshData.DataToCornerMap[p];
            _predictor.ComputePredictedValue(cornerId, data, p);
            int dstOffset = p * numComponents;
            correctionValues.SetSubArray(Transform.ComputeCorrectionValue(data.GetSubArray(dstOffset), _predictor.PredictedValue), dstOffset);
        }
        return correctionValues;
    }

    public override void EncodePredictionData(EncoderBuffer encoderBuffer)
    {
        int numOrientations = _predictor.Orientations.Count;
        encoderBuffer.WriteInt32(numOrientations);
        var lastOrientation = true;
        var encoder = new RAnsBitEncoder();
        encoder.StartEncoding(encoderBuffer);
        for (int i = 0; i < numOrientations; ++i)
        {
            var orientation = _predictor.Orientations[i];
            encoder.EncodeBit(orientation == lastOrientation);
            lastOrientation = orientation;
        }
        encoder.EndEncoding();
        base.EncodePredictionData(encoderBuffer);
    }
}
