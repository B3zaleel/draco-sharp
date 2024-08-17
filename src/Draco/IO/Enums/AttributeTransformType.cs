namespace Draco.IO.Enums;

public enum AttributeTransformType : sbyte
{
    InvalidTransform = -1,
    NoTransform = 0,
    QuantizationTransform = 1,
    OctahedronTransform = 2,
}
