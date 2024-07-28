using System.Numerics;
using Draco.IO.Core;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class PredictionSchemeNormalOctahedronCanonicalizedDecodingTransform<TDataType> : PredictionSchemeNormalOctahedronCanonicalizedDecodingTransform<TDataType, TDataType>
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
{ }

internal class PredictionSchemeNormalOctahedronCanonicalizedDecodingTransform<TDataType, TCorrectedType> : PredictionSchemeNormalOctahedronCanonicalizedTransform<TDataType>
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
    where TCorrectedType : struct,
        IComparisonOperators<TDataType, TDataType, bool>,
        IComparable,
        IEqualityOperators<TDataType, TDataType, bool>,
        IAdditionOperators<TDataType, TDataType, TDataType>,
        ISubtractionOperators<TDataType, TDataType, TDataType>,
        IDivisionOperators<TDataType, TDataType, TDataType>,
        IDecrementOperators<TDataType>,
        IBitwiseOperators<TDataType, TDataType, TDataType>,
        IMinMaxValue<TDataType>
{
    public TDataType[] ComputeOriginalValue(TDataType[] predictedValues, TCorrectedType[] correctedValues)
    {
        int[] predicted = [(int)Convert.ChangeType(predictedValues[0], typeof(int)), (int)Convert.ChangeType(predictedValues[1], typeof(int))];
        int[] corrected = [(int)Convert.ChangeType(correctedValues[0], typeof(int)), (int)Convert.ChangeType(correctedValues[1], typeof(int))];
        Assertions.ThrowIfNot(predicted[0] <= 2 * CenterValue && predicted[1] <= 2 * CenterValue && corrected[0] <= 2 * CenterValue && corrected[1] <= 2 * CenterValue);
        Assertions.ThrowIfNot(predicted[0] > 0 && predicted[1] > 0 && corrected[0] > 0 && corrected[1] > 0);
        predicted[0] = predicted[0] - CenterValue;
        predicted[1] = predicted[1] - CenterValue;
        var predIsInDiamond = IsInDiamond(predicted[0], predicted[1]);

        if (!predIsInDiamond)
        {
            InvertDiamond(ref predicted[0], ref predicted[1]);
        }
        var predIsInBottomLeft = IsInBottomLeft(predicted);
        var rotationCount = GetRotationCount(predicted);
        if (!predIsInBottomLeft)
        {
            predicted = RotatePoint(predicted, rotationCount);
        }
        int[] original = [ModMax(MathUtilities.AddAsUnsigned(predicted[0], corrected[0])), ModMax(MathUtilities.AddAsUnsigned(predicted[1], corrected[1]))];
        if (!predIsInBottomLeft)
        {
            original = RotatePoint(original, (4 - rotationCount) % 4);
        }
        if (!predIsInDiamond)
        {
            InvertDiamond(ref original[0], ref original[1]);
        }
        return [(TDataType)Convert.ChangeType(original[0] + CenterValue, typeof(TDataType)), (TDataType)Convert.ChangeType(original[1] + CenterValue, typeof(TDataType))];
    }

    public override void DecodeTransformData(DecoderBuffer decoderBuffer)
    {
        MaxQuantizedValue = (int)Convert.ChangeType(decoderBuffer.Read<TDataType>(), typeof(int));
        decoderBuffer.ReadInt32();
    }
}
