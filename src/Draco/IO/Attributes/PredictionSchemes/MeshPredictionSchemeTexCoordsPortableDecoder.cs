using System.Numerics;
using Draco.IO.BitCoders;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class MeshPredictionSchemeTexCoordsPortableDecoder<TDataType, TTransform>(PointAttribute attribute, TTransform transform, MeshPredictionSchemeData meshData) : MeshPredictionSchemeDecoder<TDataType, TTransform>(attribute, transform, meshData)
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
    private readonly MeshPredictionSchemeTexCoordsPortablePredictor<TDataType> _predictor = new(meshData, false);
    public override PredictionSchemeMethod Method { get => PredictionSchemeMethod.TexCoordsPortable; }
    public override int ParentAttributesCount { get => 1; set { } }
    public override GeometryAttributeType ParentAttributeType
    {
        get => GeometryAttributeType.Position;
    }
    public override PointAttribute? ParentAttribute
    {
        get => base.ParentAttribute;
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

    public override TDataType[] ComputeOriginalValues(TDataType[] correctedData, int _, int numComponents, List<uint> entryToPointMap)
    {
        Assertions.ThrowIf(numComponents != MeshPredictionSchemeTexCoordsPortablePredictor<TDataType>.kNumComponents);
        _predictor.EntryToPointMap = entryToPointMap;
        Transform.Init(numComponents);
        var originalValues = new TDataType[MeshData.DataToCornerMap!.Count * numComponents];

        for (int p = 0; p < MeshData.DataToCornerMap!.Count; ++p)
        {
            var cornerId = MeshData.DataToCornerMap[p];
            _predictor.ComputePredictedValue(cornerId, originalValues, p);
            var dstOffset = p * numComponents;
            originalValues.SetSubArray(Transform.ComputeOriginalValue(_predictor.PredictedValue, correctedData.GetSubArray(dstOffset)), dstOffset);
        }
        return originalValues;
    }

    public override void DecodePredictionData(DecoderBuffer decoderBuffer)
    {
        var numOrientations = decoderBuffer.ReadInt32();
        Assertions.ThrowIf(numOrientations < 0);
        var lastOrientation = true;
        var decoder = new RAnsBitDecoder();
        decoder.StartDecoding(decoderBuffer);

        for (int i = 0; i < numOrientations; ++i)
        {
            if (decoder.DecodeNextBit() == 0)
            {
                lastOrientation = !lastOrientation;
            }
            _predictor.Orientations.Add(lastOrientation);
        }
        decoder.EndDecoding(decoderBuffer);
        base.DecodePredictionData(decoderBuffer);
    }
}
