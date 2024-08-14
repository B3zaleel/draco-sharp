using System.Numerics;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class PredictionSchemeNormalOctahedronDecodingTransform<TDataType> : PredictionSchemeNormalOctahedronDecodingTransform<TDataType, TDataType>
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
{ }

internal class PredictionSchemeNormalOctahedronDecodingTransform<TDataType, TCorrectedType> : PredictionSchemeNormalOctahedronTransform<TDataType, TCorrectedType>
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
    where TCorrectedType : struct,
        IComparisonOperators<TCorrectedType, TCorrectedType, bool>,
        IComparable,
        IEqualityOperators<TCorrectedType, TCorrectedType, bool>,
        IAdditionOperators<TCorrectedType, TCorrectedType, TCorrectedType>,
        ISubtractionOperators<TCorrectedType, TCorrectedType, TCorrectedType>,
        IDivisionOperators<TCorrectedType, TCorrectedType, TCorrectedType>,
        IMultiplyOperators<TCorrectedType, TCorrectedType, TCorrectedType>,
        IDecrementOperators<TCorrectedType>,
        IBitwiseOperators<TCorrectedType, TCorrectedType, TCorrectedType>,
        IMinMaxValue<TCorrectedType>
{
    public override TDataType[] ComputeOriginalValue(TDataType[] predictedValues, TCorrectedType[] correctedValues)
    {
        int[] predicted = [(int)Convert.ChangeType(predictedValues[0], typeof(int)), (int)Convert.ChangeType(predictedValues[1], typeof(int))];
        int[] corrected = [(int)Convert.ChangeType(correctedValues[0], typeof(int)), (int)Convert.ChangeType(correctedValues[1], typeof(int))];
        Assertions.ThrowIfNot(predicted[0] <= 2 * CenterValue && predicted[1] <= 2 * CenterValue && corrected[0] <= 2 * CenterValue && corrected[1] <= 2 * CenterValue);
        Assertions.ThrowIfNot(predicted[0] > 0 && predicted[1] > 0 && corrected[0] > 0 && corrected[1] > 0);
        predicted[0] = (int)((uint)predicted[0] - (uint)CenterValue);
        predicted[1] = (int)((uint)predicted[1] - (uint)CenterValue);
        var predIsInDiamond = IsInDiamond(predicted[0], predicted[1]);

        if (!predIsInDiamond)
        {
            InvertDiamond(ref predicted[0], ref predicted[1]);
        }
        int[] original = [ModMax(predicted[0] + corrected[0]), ModMax(predicted[1] + corrected[1])];
        if (!predIsInDiamond)
        {
            InvertDiamond(ref original[0], ref original[1]);
        }
        return [(TDataType)Convert.ChangeType((uint)original[0] + (uint)CenterValue, typeof(TDataType)), (TDataType)Convert.ChangeType((uint)original[1] + (uint)CenterValue, typeof(TDataType))];
    }

    public override void DecodeTransformData(DecoderBuffer decoderBuffer)
    {
        MaxQuantizedValue = (int)Convert.ChangeType(decoderBuffer.Read<TDataType>(), typeof(int));
        if (decoderBuffer.BitStreamVersion < Constants.BitStreamVersion(2, 2))
        {
            decoderBuffer.ReadInt32();
        }
    }
}
