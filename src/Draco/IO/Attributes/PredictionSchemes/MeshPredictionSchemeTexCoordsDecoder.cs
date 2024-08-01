using System.Net.Http.Headers;
using System.Numerics;
using Draco.IO.BitCoders;
using Draco.IO.Core;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class MeshPredictionSchemeTexCoordsDecoder<TDataType, TTransform>(PointAttribute attribute, TTransform transform, MeshPredictionSchemeData meshData) : MeshPredictionSchemeDecoder<TDataType, TTransform>(attribute, transform, meshData)
    where TDataType : struct,
        IComparisonOperators<TDataType, TDataType, bool>,
        IComparable,
        IEqualityOperators<TDataType, TDataType, bool>,
        IAdditionOperators<TDataType, TDataType, TDataType>,
        ISubtractionOperators<TDataType, TDataType, TDataType>,
        IDivisionOperators<TDataType, TDataType, TDataType>,
        IDecrementOperators<TDataType>,
        IBitwiseOperators<TDataType, TDataType, TDataType>,
        IMinMaxValue<TDataType>
    where TTransform : PredictionSchemeDecodingTransform<TDataType>
{
    private PointAttribute? _posAttribute;
    private List<uint> _entryToPointMap = [];
    private TDataType[] _predictedValues = [];
    private int _numComponents = 0;
    private readonly List<bool> _orientations = [];
    private ushort _version = 0;

    public override PredictionSchemeMethod Method { get => PredictionSchemeMethod.TexCoordsDeprecated; }
    protected override int NumParentAttribute { get => 1; set => base.NumParentAttribute = value; }
    public override GeometryAttributeType ParentAttributeType
    {
        get => GeometryAttributeType.Position;
    }
    public override PointAttribute? ParentAttribute
    {
        get => _posAttribute;
        set
        {
            Assertions.ThrowIf(value!.AttributeType != GeometryAttributeType.Position);
            Assertions.ThrowIf(value.NumComponents != 3);
            base.ParentAttribute = value;
            _posAttribute = value;
        }
    }

    public bool IsInitialized()
    {
        return _posAttribute != null && MeshData.IsInitialized();
    }

    public override TDataType[] ComputeOriginalValues(TDataType[] correctedData, int size, int numComponents, List<uint> entryToPointMap)
    {
        Assertions.ThrowIf(numComponents != 2);
        _numComponents = numComponents;
        _entryToPointMap = entryToPointMap;
        _predictedValues = new TDataType[numComponents];
        var originalValues = new TDataType[numComponents];
        Transform.Init(numComponents);

        for (int p = 0; p < MeshData.DataToCornerMap!.Count; ++p)
        {
            ComputePredictedValue(MeshData.DataToCornerMap[p], originalValues, p);
            var dstOffset = p * numComponents;
            originalValues.SetSubArray(Transform.ComputeOriginalValue(_predictedValues, correctedData.GetSubArray(dstOffset)), dstOffset);
        }
        return originalValues;
    }

    public new void DecodePredictionData(DecoderBuffer decoderBuffer)
    {
        var numOrientations = decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 2) ? decoderBuffer.ReadUInt32() : (uint)decoderBuffer.DecodeVarIntUnsigned();
        Assertions.ThrowIf(numOrientations < 0);
        Assertions.ThrowIf(numOrientations > MeshData.CornerTable!.CornersCount);
        _orientations.Fill((int)numOrientations, true);
        var lastOrientation = true;
        var decoder = new RAnsBitDecoder();
        decoder.StartDecoding(decoderBuffer);
        _version = decoderBuffer.BitStream_Version;

        for (int i = 0; i < numOrientations; ++i)
        {
            if (decoder.DecodeNextBit() == 0)
            {
                lastOrientation = !lastOrientation;
            }
            _orientations[i] = lastOrientation;
        }
        decoder.EndDecoding(decoderBuffer);
        base.DecodePredictionData(decoderBuffer);
    }

    private void ComputePredictedValue(uint cornerId, TDataType[] originalValues, int dataId)
    {
        var nextCornerId = MeshData.CornerTable!.Next(cornerId);
        var previousCornerId = MeshData.CornerTable.Previous(cornerId);
        var nextVertexId = MeshData.CornerTable.Vertex(nextCornerId);
        var previousVertexId = MeshData.CornerTable.Vertex(previousCornerId);
        var nextDataId = MeshData.VertexToDataMap![(int)nextVertexId];
        var previousDataId = MeshData.VertexToDataMap[(int)previousVertexId];

        if (previousDataId < dataId && nextDataId < dataId)
        {
            var nUV = GetTexCoordForEntryId(nextDataId, originalValues);
            var pUV = GetTexCoordForEntryId(previousDataId, originalValues);

            if (pUV == nUV)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (float.IsNaN(pUV.Components[i]) || pUV.Components[i] > (double)Convert.ChangeType(TDataType.MaxValue, typeof(double)) || pUV.Components[i] < (double)Convert.ChangeType(TDataType.MinValue, typeof(double)))
                    {
                        _predictedValues[i] = TDataType.MinValue;
                    }
                    else
                    {
                        _predictedValues[i] = (TDataType)Convert.ChangeType(pUV.Components[i], typeof(TDataType));
                    }
                }
                return;
            }
            var tipPosition = GetPositionForEntryId(dataId);
            var nextPosition = GetPositionForEntryId(nextDataId);
            var previousPosition = GetPositionForEntryId(previousDataId);
            var pn = previousPosition - nextPosition;
            var cn = tipPosition - nextPosition;
            var pnNorm2Squared = pn.SquaredNorm();
            float s, t;

            if (_version < Constants.BitStreamVersion(2, 2) || pnNorm2Squared > 0)
            {
                s = pn.Dot(cn) / pnNorm2Squared;
                t = (float)Math.Sqrt((cn - pn * s).SquaredNorm() / pnNorm2Squared);
            }
            else
            {
                s = 0.0f;
                t = 0.0f;
            }
            var pnUV = pUV - nUV;
            var pnUS = pnUV.Components[0] * s + nUV.Components[0];
            var pnUT = pnUV.Components[0] * t;
            var pnVS = pnUV.Components[1] * s + nUV.Components[1];
            var pnVT = pnUV.Components[1] * t;
            Assertions.ThrowIf(_orientations.Count == 0);
            var orientation = _orientations.Last();
            _orientations.PopBack();
            var predictedUV = orientation
                ? new Vector2<float>(pnUS - pnVT, pnVS + pnUT)
                : new Vector2<float>(pnUS + pnVT, pnVS - pnUT);

            if (Constants.IsIntegral<TDataType>())
            {
                var u = Math.Floor(predictedUV.Components[0] + 0.5);
                if (float.IsNaN((float)u) || u > (double)Convert.ChangeType(TDataType.MaxValue, typeof(double)) || u < (double)Convert.ChangeType(TDataType.MinValue, typeof(double)))
                {
                    _predictedValues[0] = TDataType.MinValue;
                }
                else
                {
                    _predictedValues[0] = (TDataType)Convert.ChangeType((int)u, typeof(TDataType));
                }

                var v = Math.Floor(predictedUV.Components[1] + 0.5);
                if (float.IsNaN((float)v) || v > (double)Convert.ChangeType(TDataType.MaxValue, typeof(double)) || v < (double)Convert.ChangeType(TDataType.MinValue, typeof(double)))
                {
                    _predictedValues[1] = TDataType.MinValue;
                }
                else
                {
                    _predictedValues[1] = (TDataType)Convert.ChangeType((int)v, typeof(TDataType));
                }
            }
            else
            {
                _predictedValues[0] = (TDataType)Convert.ChangeType((int)predictedUV.Components[0], typeof(TDataType));
                _predictedValues[1] = (TDataType)Convert.ChangeType((int)predictedUV.Components[1], typeof(TDataType));
            }
            return;
        }

        var dataOffset = 0;
        if (previousDataId < dataId)
        {
            dataOffset = previousDataId * _numComponents;
        }
        if (nextDataId < dataId)
        {
            dataOffset = nextDataId * _numComponents;
        }
        else
        {
            if (dataId > 0)
            {
                dataOffset = (dataId - 1) * _numComponents;
            }
            else
            {
                for (int i = 0; i < _numComponents; ++i)
                {
                    _predictedValues[i] = default;
                }
                return;
            }
        }
        for (int i = 0; i < _numComponents; ++i)
        {
            _predictedValues[i] = originalValues[dataOffset + i];
        }
    }

    private Vector3<float> GetPositionForEntryId(int entryId)
    {
        var pointId = _entryToPointMap[entryId];
        var values = _posAttribute!.ConvertValue<float>(_posAttribute.MappedIndex(pointId));
        return new Vector3<float>((float)Convert.ChangeType(values[0], typeof(float)), (float)Convert.ChangeType(values[1], typeof(float)), (float)Convert.ChangeType(values[2], typeof(float)));
    }

    private Vector2<float> GetTexCoordForEntryId(int entryId, TDataType[] data)
    {
        var dataOffset = entryId * _numComponents;
        return new Vector2<float>((float)Convert.ChangeType(data[dataOffset], typeof(float)), (float)Convert.ChangeType(data[dataOffset + 1], typeof(float)));
    }
}
