namespace Draco.IO.Enums;

public enum PredictionSchemeMethod : sbyte
{
    None = -2,
    Undefined = -1,
    Difference = 0,
    Parallelogram = 1,
    MultiParallelogram = 2,
    TexCoordsDeprecated = 3,
    ConstrainedMultiParallelogram = 4,
    TexCoordsPortable = 5,
    GeometricNormal = 6,
    Count
}
