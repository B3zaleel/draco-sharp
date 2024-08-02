using Draco.IO.Extensions;

namespace Draco.IO.Attributes;

public class AttributeTransformData
{
    public AttributeTransformType TransformType { get; set; } = AttributeTransformType.InvalidTransform;
    public Stream? Buffer { get; set; } = new MemoryStream();

    public TDataType GetParameterValue<TDataType>()
    {
        return Buffer!.ReadDatum<TDataType>();
    }

    public void SetParameterValue<TDataType>(TDataType data)
    {
        Buffer!.WriteData([data]);
    }

    public void AppendParameterValue<TDataType>(TDataType data)
    {
        SetParameterValue(data);
    }
}
