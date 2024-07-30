using System.Numerics;
using Draco.IO.BitCoders;
using Draco.IO.Core;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class MeshPredictionSchemeConstrainedMultiParallelogramDecoder<TDataType, TTransform>(PointAttribute attribute, TTransform transform, MeshPredictionSchemeData meshData) : MeshPredictionSchemeDecoder<TDataType, TTransform>(attribute, transform, meshData)
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
    private const int kMaxNumParallelograms = 4;
    private MultiParallelogramMode _selectedMode = MultiParallelogramMode.Optimal;
    private readonly List<bool>[] _isCreaseEdge = new List<bool>[kMaxNumParallelograms];

    public override PredictionSchemeMethod Method { get => PredictionSchemeMethod.ConstrainedMultiParallelogram; }

    public override TDataType[] ComputeOriginalValues(TDataType[] correctedData, int _, int numComponents, List<uint> __)
    {
        Transform.Init(numComponents);
        var predictedValues = new List<TDataType>[kMaxNumParallelograms];
        var originalValues = new TDataType[MeshData.DataToCornerMap!.Count * numComponents];

        for (int i = 0; i < kMaxNumParallelograms; ++i)
        {
            predictedValues[i] = new List<TDataType>(correctedData.Length);
            predictedValues[i].Fill(correctedData.Length, (TDataType)default);
        }
        var isCreaseEdgePos = new int[kMaxNumParallelograms];
        Array.Fill(isCreaseEdgePos, 0);
        var multiPredValues = new TDataType[numComponents];
        Array.Fill(multiPredValues, default);
        originalValues.SetSubArray(Transform.ComputeOriginalValue(originalValues.GetSubArray(0), correctedData.GetSubArray(0)), 0);

        for (int p = 1; p < MeshData.DataToCornerMap!.Count; ++p)
        {
            var startCornerId = MeshData.DataToCornerMap[p];
            var cornerId = startCornerId;
            int numParallelograms = 0;
            var firstPass = true;

            while (cornerId != Constants.kInvalidCornerIndex)
            {
                if (MeshPredictionSchemeParallelogramDecoder<TDataType, TTransform>.TryComputeParallelogramPrediction(p, cornerId, MeshData.CornerTable!, MeshData.VertexToDataMap!, originalValues, numComponents, out TDataType[] parallelogramPredictedValues))
                {
                    ++numParallelograms;
                    if (numParallelograms == kMaxNumParallelograms)
                    {
                        break;
                    }
                }
                cornerId = firstPass ? MeshData.CornerTable!.SwingLeft(cornerId) : MeshData.CornerTable!.SwingRight(cornerId);
                if (cornerId == startCornerId)
                {
                    break;
                }
                if (cornerId == Constants.kInvalidCornerIndex && firstPass)
                {
                    firstPass = false;
                    cornerId = MeshData.CornerTable!.SwingRight(startCornerId);
                }
            }
            int numUsedParallelograms = 0;
            if (numParallelograms > 0)
            {
                for (int i = 0; i < numComponents; ++i)
                {
                    multiPredValues[i] = (TDataType)Convert.ChangeType(0, typeof(TDataType));
                }
                for (int i = 0; i < numParallelograms; ++i)
                {
                    var context = numParallelograms - 1;
                    int pos = isCreaseEdgePos[context]++;
                    Assertions.ThrowIf(_isCreaseEdge[context].Count <= pos);
                    if (!_isCreaseEdge[context][pos])
                    {
                        ++numUsedParallelograms;
                        for (int j = 0; j < numComponents; ++j)
                        {
                            multiPredValues[j] = MathUtilities.AddAsUnsigned(multiPredValues[j], predictedValues[j][j]);
                        }
                    }
                }
            }
            int dstOffset = p * numComponents;
            if (numUsedParallelograms == 0)
            {
                var srcOffset = (p - 1) * numComponents;
                originalValues.SetSubArray(Transform.ComputeOriginalValue(originalValues.GetSubArray(srcOffset), correctedData.GetSubArray(dstOffset)), dstOffset);
            }
            else
            {
                for (int c = 0; c < numComponents; ++c)
                {
                    multiPredValues[c] /= (TDataType)Convert.ChangeType(numUsedParallelograms, typeof(TDataType));
                }
                originalValues.SetSubArray(Transform.ComputeOriginalValue(multiPredValues, correctedData.GetSubArray(dstOffset)), dstOffset);
            }
        }
        return originalValues;
    }

    public void DecodeTransformData(DecoderBuffer decoderBuffer)
    {
        if (decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 2))
        {
            _selectedMode = (MultiParallelogramMode)decoderBuffer.ReadByte();
        }
        for (int i = 0; i < kMaxNumParallelograms; ++i)
        {
            var numFlags = (uint)decoderBuffer.DecodeVarIntUnsigned();
            if (numFlags > 0)
            {
                _isCreaseEdge[i] = new List<bool>((int)numFlags);
                var decoder = new RAnsBitDecoder();
                decoder.StartDecoding(decoderBuffer);

                for (uint j = 0; j < numFlags; ++j)
                {
                    _isCreaseEdge[i].Add(decoder.DecodeNextBit() != 0);
                }
                decoder.EndDecoding();
            }
        }
        base.DecodePredictionData(decoderBuffer);
    }
}
