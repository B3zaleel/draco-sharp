namespace Draco.IO.Attributes.PredictionSchemes;

internal interface IPredictionScheme
{
    public PredictionSchemeMethod Method { get; }
    public PredictionSchemeTransformType TransformType { get; set; }
    public PointAttribute Attribute { get; set; }
    public PointAttribute? ParentAttribute { get; set; }
    public int ParentAttributesCount { get; set; }
    public GeometryAttributeType ParentAttributeType { get; set; }

    public virtual bool IsInitialized()
    {
        return true;
    }

    public virtual bool AreCorrectionsPositive()
    {
        return true;
    }

    public GeometryAttributeType GetParentAttributeType(int i);
}
