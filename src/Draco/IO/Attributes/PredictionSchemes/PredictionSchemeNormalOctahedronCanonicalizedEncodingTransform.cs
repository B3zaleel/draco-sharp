using System.Numerics;
using Draco.IO.Enums;
using Draco.IO.Extensions;

namespace Draco.IO.Attributes.PredictionSchemes;

internal class PredictionSchemeNormalOctahedronCanonicalizedEncodingTransform<TDataType> : PredictionSchemeNormalOctahedronCanonicalizedEncodingTransform<TDataType, TDataType>
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
{
    public PredictionSchemeNormalOctahedronCanonicalizedEncodingTransform() { }

    public PredictionSchemeNormalOctahedronCanonicalizedEncodingTransform(int maxQuantizedValue) : base(maxQuantizedValue) { }
}

internal class PredictionSchemeNormalOctahedronCanonicalizedEncodingTransform<TDataType, TCorrectedType> : PredictionSchemeNormalOctahedronCanonicalizedTransform<TDataType, TCorrectedType>, IPredictionSchemeEncodingTransform<TDataType, TCorrectedType>
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
    public int ComponentsCount { get; set; }
    public int QuantizationBits { get; }
    public PredictionSchemeTransformType Type { get => PredictionSchemeTransformType.NormalOctahedronCanonicalized; }

    public PredictionSchemeNormalOctahedronCanonicalizedEncodingTransform() { }

    public PredictionSchemeNormalOctahedronCanonicalizedEncodingTransform(int maxQuantizedValue)
    {
        MaxQuantizedValue = maxQuantizedValue;
    }

    public virtual void Init(TDataType[] originalData, int size, int componentsCount) { }

    public TCorrectedType[] ComputeCorrectionValue(TDataType[] originalValues, TDataType[] predictedValues)
    {
        int[] original = [(int)Convert.ChangeType(originalValues[0], typeof(int)) - CenterValue, (int)Convert.ChangeType(originalValues[1], typeof(int)) - CenterValue];
        int[] predicted = [(int)Convert.ChangeType(predictedValues[0], typeof(int)) - CenterValue, (int)Convert.ChangeType(predictedValues[1], typeof(int)) - CenterValue];
        Assertions.ThrowIfNot(predicted[0] <= CenterValue * 2 && predicted[1] <= CenterValue * 2, "Predicted values are out of bounds");
        Assertions.ThrowIfNot(original[0] <= CenterValue * 2 && original[1] <= CenterValue * 2, "Original values are out of bounds");

        if (IsInDiamond(predicted[0], predicted[1]))
        {
            InvertDiamond(ref original[0], ref original[1]);
            InvertDiamond(ref predicted[0], ref predicted[1]);
        }
        if (IsInBottomLeft(predicted))
        {
            var rotationCount = GetRotationCount(predicted);
            original = RotatePoint(original, rotationCount);
            predicted = RotatePoint(predicted, rotationCount);
        }

        var corrected = new TCorrectedType[2] { (TCorrectedType)Convert.ChangeType(MakePositive(original[0] - predicted[0]), typeof(TCorrectedType)), (TCorrectedType)Convert.ChangeType(MakePositive(original[1] - predicted[1]), typeof(TCorrectedType)) };
        return corrected;
    }

    public void EncodeTransformData(EncoderBuffer encoderBuffer)
    {
        encoderBuffer.WriteInt32(MaxQuantizedValue);
        encoderBuffer.WriteInt32(CenterValue);
    }
}
