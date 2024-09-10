using System.Numerics;
using Draco.IO.BitCoders;
using Draco.IO.Core;
using Draco.IO.Entropy;
using Draco.IO.Enums;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class MeshPredictionSchemeConstrainedMultiParallelogramEncoder<TDataType, TTransform>(PointAttribute attribute, TTransform transform, MeshPredictionSchemeData meshData) : MeshPredictionSchemeEncoder<TDataType, TTransform>(attribute, transform, meshData)
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
    // private readonly MultiParallelogramMode _selectedMode = MultiParallelogramMode.Optimal;
    private readonly List<bool>[] _isCreaseEdge = new List<bool>[Constants.ConstrainedMultiParallelogramMaxNumParallelograms];
    private readonly ShannonEntropyTracker _entropyTracker = new();
    private uint[] _entropySymbols = [];
    public override PredictionSchemeMethod Method { get => PredictionSchemeMethod.ConstrainedMultiParallelogram; }

    public bool IsInitialized()
    {
        return MeshData.IsInitialized();
    }

    public override TDataType[] ComputeCorrectionValues(TDataType[] data, int size, int numComponents, List<uint> _)
    {
        Transform.Init(data, size, numComponents);
        var correctionValues = new TDataType[size];
        var predictedValues = new List<TDataType>[Constants.ConstrainedMultiParallelogramMaxNumParallelograms];
        for (int i = 0; i < Constants.ConstrainedMultiParallelogramMaxNumParallelograms; ++i)
        {
            predictedValues[i] = new List<TDataType>(numComponents);
            predictedValues[i].Fill(numComponents, (TDataType)default);
        }
        var multiPredValues = new TDataType[numComponents];
        _entropySymbols = new uint[numComponents];
        var excludedParallelograms = new bool[Constants.ConstrainedMultiParallelogramMaxNumParallelograms];
        var totalUsedParallelograms = new long[Constants.ConstrainedMultiParallelogramMaxNumParallelograms];
        var totalParallelograms = new long[Constants.ConstrainedMultiParallelogramMaxNumParallelograms];
        var currentResiduals = new int[numComponents];

        for (int p = MeshData.DataToCornerMap!.Count - 1; p > 0; --p)
        {
            var startCornerId = MeshData.DataToCornerMap[p];
            var cornerId = startCornerId;
            int numParallelograms = 0;
            var firstPass = true;

            while (cornerId != Constants.kInvalidCornerIndex)
            {
                if (MeshPredictionSchemeParallelogramDecoder<TDataType, IPredictionSchemeDecodingTransform<TDataType, TDataType>>.TryComputeParallelogramPrediction(p, cornerId, MeshData.CornerTable!, MeshData.VertexToDataMap!, data, numComponents, out TDataType[] parallelogramPredictedData))
                {
                    predictedValues[numParallelograms] = new List<TDataType>(parallelogramPredictedData);
                    ++numParallelograms;
                    if (numParallelograms == Constants.ConstrainedMultiParallelogramMaxNumParallelograms)
                    {
                        break;
                    }
                }

                if (firstPass)
                {
                    cornerId = MeshData.CornerTable!.SwingLeft(cornerId);
                }
                else
                {
                    cornerId = MeshData.CornerTable!.SwingRight(cornerId);
                }
                if (cornerId == startCornerId)
                {
                    break;
                }
                if (cornerId == Constants.kInvalidCornerIndex && firstPass)
                {
                    firstPass = false;
                    cornerId = MeshData.CornerTable.SwingRight(startCornerId);
                }
            }
            int dstOffset = p * numComponents;
            int srcOffset = (p - 1) * numComponents;
            var error = ComputeError(data.GetSubArray(srcOffset, numComponents), data.GetSubArray(dstOffset, numComponents), out currentResiduals, numComponents);
            if (numParallelograms > 0)
            {
                totalParallelograms[numParallelograms - 1] += numParallelograms;
                var newOverheadBits = ComputeOverheadBits(totalUsedParallelograms[numParallelograms - 1], totalParallelograms[numParallelograms - 1]);
                error.NumBits += (int)newOverheadBits;
            }
            PredictionConfiguration bestPrediction = new(error: error, configuration: 0, numUsedParallelograms: 0, predictedValue: new Core.Vector<TDataType>(data.GetSubArray(srcOffset, numComponents)), residuals: new Core.Vector<int>(currentResiduals));
            for (int numUsedParallelograms = 1; numUsedParallelograms <= numParallelograms; ++numUsedParallelograms)
            {
                Array.Fill(excludedParallelograms, true, numParallelograms, excludedParallelograms.Length - numParallelograms);
                for (int j = 0; j < numUsedParallelograms; ++j)
                {
                    excludedParallelograms[j] = false;
                }
                do
                {
                    for (int j = 0; j < numComponents; ++j)
                    {
                        multiPredValues[j] = Constants.ConstCast<int, TDataType>(0);
                    }
                    byte configuration = 0;
                    for (int j = 0; j < numParallelograms; ++j)
                    {
                        if (excludedParallelograms[j])
                        {
                            continue;
                        }
                        for (int c = 0; c < numComponents; ++c)
                        {
                            multiPredValues[c] += predictedValues[j][c];
                        }
                        configuration |= (byte)(1 << j);
                    }
                    for (int j = 0; j < numComponents; ++j)
                    {
                        multiPredValues[j] /= Constants.ConstCast<int, TDataType>(numUsedParallelograms);
                    }
                    error = ComputeError(multiPredValues, data.GetSubArray(dstOffset, numComponents), out currentResiduals, numComponents);
                    long newOverheadBits = ComputeOverheadBits(totalUsedParallelograms[numParallelograms - 1] + numUsedParallelograms, totalParallelograms[numParallelograms - 1]);
                    error.NumBits += (int)newOverheadBits;
                    if (error.IsLessThan(bestPrediction.Error))
                    {
                        bestPrediction.Error = error;
                        bestPrediction.Configuration = configuration;
                        bestPrediction.NumUsedParallelograms = numUsedParallelograms;
                        bestPrediction.PredictedValue = new Core.Vector<TDataType>(multiPredValues);
                        bestPrediction.Residuals = new Core.Vector<int>(currentResiduals);
                    }
                } while (Constants.NextPermutation(excludedParallelograms));
            }
            if (numParallelograms > 0)
            {
                totalUsedParallelograms[numParallelograms - 1] += bestPrediction.NumUsedParallelograms;
            }
            for (int i = 0; i < numComponents; ++i)
            {
                _entropySymbols[i] = BitUtilities.ConvertSignedIntToSymbol(bestPrediction.Residuals[i]);
            }
            _entropyTracker.Push(_entropySymbols, numComponents);
            for (int i = 0; i < numParallelograms; ++i)
            {
                _isCreaseEdge[numParallelograms - 1].Add((bestPrediction.Configuration & (1 << i)) == 0);
            }
            correctionValues.SetSubArray(Transform.ComputeCorrectionValue(data.GetSubArray(dstOffset, numComponents), bestPrediction.PredictedValue.Components), dstOffset);
        }
        for (int i = 0; i < numComponents; ++i)
        {
            predictedValues[0][i] = Constants.ConstCast<int, TDataType>(0);
        }
        correctionValues.SetSubArray(Transform.ComputeCorrectionValue(data.GetSubArray(0, numComponents), predictedValues[0].ToArray()), 0);
        return correctionValues;
    }

    private static long ComputeOverheadBits(long totalUsedParallelograms, long totalParallelograms)
    {
        var entropy = ShannonEntropy.ComputeBinaryShannonEntropy(Constants.ConstCast<long, uint>(totalUsedParallelograms), Constants.ConstCast<long, uint>(totalParallelograms));
        return (long)Math.Ceiling(totalParallelograms * entropy);
    }

    private Error ComputeError(TDataType[] predictedValues, TDataType[] actualValues, out int[] residuals, int numComponents)
    {
        var error = new Error();
        residuals = new int[numComponents];

        for (int i = 0; i < numComponents; ++i)
        {
            var diff = Constants.ConstCast<TDataType, int>(predictedValues[i] - actualValues[i]);
            error.ResidualError += Math.Abs(diff);
            residuals[i] = diff;
            _entropySymbols[i] = BitUtilities.ConvertSignedIntToSymbol(diff);
        }
        var entropyData = _entropyTracker.Peek(_entropySymbols, numComponents);
        error.NumBits = (int)(ShannonEntropy.GetNumberOfDataBits(entropyData) + ShannonEntropy.GetNumberOfRAnsTableBits(entropyData));
        return error;
    }

    public override void EncodePredictionData(EncoderBuffer encoderBuffer)
    {
        for (byte i = 0; i < Constants.ConstrainedMultiParallelogramMaxNumParallelograms; ++i)
        {
            int numUsedParallelograms = i + 1;
            encoderBuffer.EncodeVarIntUnsigned((uint)_isCreaseEdge[i].Count);

            if (_isCreaseEdge[i].Count != 0)
            {
                var encoder = new RAnsBitEncoder();
                encoder.StartEncoding();
                for (int j = _isCreaseEdge[i].Count - numUsedParallelograms; j >= 0; j -= numUsedParallelograms)
                {
                    for (int k = 0; k < numUsedParallelograms; ++k)
                    {
                        encoder.EncodeBit(_isCreaseEdge[i][j + k]);
                    }
                }
                encoder.EndEncoding(encoderBuffer);
            }
        }
        base.EncodePredictionData(encoderBuffer);
    }

    /// <summary>
    /// Represents an object that contains data used for measuring the error of each available parallelogram configuration.
    /// </summary>
    private class Error
    {
        /// <summary>
        /// Primary metric: number of bits required to store the data as a result of the selected prediction configuration.
        /// </summary>
        /// <value></value>
        public int NumBits { get; set; } = 0;
        /// <summary>
        /// Secondary metric: absolute difference of residuals for the given configuration.
        /// </summary>
        /// <value></value>
        public int ResidualError { get; set; } = 0;

        public bool IsLessThan(Error error)
        {
            return NumBits < error.NumBits && ResidualError < error.ResidualError;
        }
    }

    private class PredictionConfiguration
    {
        public Error Error { get; set; }
        public byte Configuration { get; set; }
        public int NumUsedParallelograms { get; set; }
        public Core.Vector<TDataType> PredictedValue { get; set; }
        public Core.Vector<int> Residuals { get; set; }

        public PredictionConfiguration(Error error, byte configuration, int numUsedParallelograms, Core.Vector<TDataType> predictedValue, Core.Vector<int> residuals)
        {
            Error = error;
            Configuration = configuration;
            NumUsedParallelograms = numUsedParallelograms;
            PredictedValue = predictedValue;
            Residuals = residuals;
        }
    }
}
