using System.Numerics;

namespace Draco.IO.Attributes.PredictionSchemes;

internal abstract class PredictionSchemeDecoder<TDataType, TTransform>(PointAttribute attribute, TTransform transform) : IPredictionSchemeDecoder<TDataType, TDataType>
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
    public abstract PredictionSchemeMethod Method { get; }
    public TTransform Transform { get; set; } = transform;
    public PredictionSchemeTransformType TransformType { get; set; }
    public PointAttribute Attribute { get; set; } = attribute;
    protected virtual int NumParentAttribute { get; set; } = 0;
    public PointAttribute? ParentAttribute { get; set; }
    public int ParentAttributesCount { get; set; }
    public virtual GeometryAttributeType ParentAttributeType { get; set; } = GeometryAttributeType.Invalid;

    public virtual void DecodePredictionData(DecoderBuffer decoderBuffer)
    {
        Transform.DecodeTransformData(decoderBuffer);
    }

    protected bool AreCorrectionsPositive()
    {
        return Transform.AreCorrectionsPositive();
    }

    public GeometryAttributeType GetParentAttributeType(int i)
    {
        return GeometryAttributeType.Invalid;
    }

    public abstract TDataType[] ComputeOriginalValues(TDataType[] data, int size, int numComponents, List<uint> entryToPointMap);
}
