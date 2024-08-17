namespace Draco.IO.Enums;

public enum PredictionSchemeTransformType : sbyte
{
    None = -1,
    Delta = 0,
    Wrap = 1,
    NormalOctahedron = 2,
    NormalOctahedronCanonicalized = 3,
    Count,
}
