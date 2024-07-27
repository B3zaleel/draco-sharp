using Draco.IO.Extensions;

namespace Draco.IO.Attributes;

public class AttributeTransformData
{
    public AttributeTransformType TransformType { get; set; } = AttributeTransformType.InvalidTransform;
    public Stream? Buffer { get; set; }

    public TDataType GetParameterValue<TDataType>(int byteOffset)
    {
        Buffer!.Seek(byteOffset, SeekOrigin.Begin);
        return Buffer.ReadDatum<TDataType>();
    }

    public void SetParameterValue<TDataType>(int byteOffset, TDataType data)
    {
        Buffer!.Seek(byteOffset, SeekOrigin.Begin);
        Buffer.WriteData([data]);
    }

    public void AppendParameterValue<TDataType>(TDataType data)
    {
        SetParameterValue((int)Buffer!.Length, data);
    }
}
