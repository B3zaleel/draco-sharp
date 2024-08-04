using System.Numerics;

namespace Draco.IO.Attributes.PredictionSchemes;

internal abstract class MeshPredictionSchemeDecoder<TDataType, TTransform>(PointAttribute attribute, TTransform transform, MeshPredictionSchemeData meshData) : PredictionSchemeDecoder<TDataType, TTransform>(attribute, transform)
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
    where TTransform : PredictionSchemeDecodingTransform<TDataType, TDataType>
{
    protected MeshPredictionSchemeData MeshData { get; set; } = meshData;
}
