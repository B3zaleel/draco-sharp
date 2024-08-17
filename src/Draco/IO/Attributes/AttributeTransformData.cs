using Draco.IO.Core;
using Draco.IO.Enums;

namespace Draco.IO.Attributes;

public class AttributeTransformData
{
    public AttributeTransformType TransformType { get; set; } = AttributeTransformType.InvalidTransform;
    public DataBuffer? Buffer { get; set; } = new();

    public TDataType GetParameterValue<TDataType>(int byteOffset)
    {
        return Buffer!.Read<TDataType>(byteOffset);
    }

    public void SetParameterValue<TDataType>(TDataType data, int byteOffset)
    {
        if (byteOffset + Constants.SizeOf<TDataType>() > Buffer!.DataSize)
        {
            Buffer.Resize(byteOffset + Constants.SizeOf<TDataType>());
        }
        Buffer.Write([data], byteOffset);
    }

    public void AppendParameterValue<TDataType>(TDataType data)
    {
        SetParameterValue(data, (int)Buffer!.DataSize);
    }
}
