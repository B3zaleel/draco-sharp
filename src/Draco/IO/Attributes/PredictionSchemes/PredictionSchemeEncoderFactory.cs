using System.Numerics;
using Draco.IO.Enums;
using Draco.IO.Mesh;

namespace Draco.IO.Attributes.PredictionSchemes;

internal static class PredictionSchemeEncoderFactory
{
    public static PredictionSchemeMethod SelectPredictionMethod(ConnectivityEncoder connectivityEncoder, int attributeId)
    {
        return SelectPredictionMethod(connectivityEncoder, connectivityEncoder.Config, attributeId);
    }

    public static PredictionSchemeMethod SelectPredictionMethod(ConnectivityEncoder connectivityEncoder, Config config, int attributeId)
    {
        if (config.Speed >= 10)
        {
            return PredictionSchemeMethod.Difference;
        }
        if (connectivityEncoder.GeometryType == Constants.EncodingType.TriangularMesh)
        {
            var attributeQuantizationBits = config.GetAttributeOption(attributeId, ConfigOptionName.Attribute.QuantizationBits, -1);
            var attribute = connectivityEncoder.PointCloud!.GetAttributeById(attributeId);

            if (attributeQuantizationBits != -1 && attribute.AttributeType == GeometryAttributeType.TexCoord && attribute.NumComponents == 2)
            {
                var positionAttribute = connectivityEncoder.PointCloud.GetNamedAttribute(GeometryAttributeType.Position);
                var isPositionAttributeValid = false;
                if (positionAttribute != null)
                {
                    if (Constants.IsDataTypeIntegral(positionAttribute.DataType))
                    {
                        isPositionAttributeValid = true;
                    }
                    else
                    {
                        var positionAttributeId = connectivityEncoder.PointCloud.GetNamedAttributeId(GeometryAttributeType.Position);
                        var positionQuantizationBits = config.GetAttributeOption(positionAttributeId, ConfigOptionName.Attribute.QuantizationBits, -1);
                        if (positionQuantizationBits > 0 && positionQuantizationBits <= 21 && 2 * positionQuantizationBits + attributeQuantizationBits < 64)
                        {
                            isPositionAttributeValid = true;
                        }
                    }
                }
                if (isPositionAttributeValid && config.Speed < 4)
                {
                    return PredictionSchemeMethod.TexCoordsPortable;
                }
            }
            if (attribute.AttributeType == GeometryAttributeType.Normal)
            {
                if (config.Speed < 4)
                {
                    var positionAttributeId = connectivityEncoder.PointCloud.GetNamedAttributeId(GeometryAttributeType.Position);
                    var positionAttribute = connectivityEncoder.PointCloud.GetNamedAttribute(GeometryAttributeType.Position);
                    if (positionAttribute != null && (Constants.IsDataTypeIntegral(positionAttribute.DataType) || config.GetAttributeOption(positionAttributeId, ConfigOptionName.Attribute.QuantizationBits, -1) > 0))
                    {
                        return PredictionSchemeMethod.GeometricNormal; ;
                    }
                }
                return PredictionSchemeMethod.Difference;
            }
            if (config.Speed >= 8)
            {
                return PredictionSchemeMethod.Difference;
            }
            if (config.Speed >= 2 || connectivityEncoder.PointCloud!.PointsCount < 40)
            {
                return PredictionSchemeMethod.Parallelogram;
            }
            return PredictionSchemeMethod.ConstrainedMultiParallelogram;
        }
        return PredictionSchemeMethod.Difference;
    }

    public static PredictionSchemeMethod GetPredictionMethod(Config config, int attributeId)
    {
        var predictionType = config.GetAttributeOption(attributeId, ConfigOptionName.Attribute.PredictionScheme, -1);
        if (predictionType == -1)
        {
            return PredictionSchemeMethod.Undefined;
        }
        if (predictionType < 0 || predictionType >= (int)PredictionSchemeMethod.Count)
        {
            return PredictionSchemeMethod.None;
        }
        return (PredictionSchemeMethod)predictionType;
    }

    public static IPredictionSchemeEncoder<TDataType>? CreatePredictionScheme<TDataType, TTransform>(PredictionSchemeMethod method, int attributeId, ConnectivityEncoder connectivityEncoder, TTransform transform)
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
        where TTransform : IPredictionSchemeEncodingTransform<TDataType, TDataType>
    {
        if (method == PredictionSchemeMethod.Undefined)
        {
            method = SelectPredictionMethod(connectivityEncoder, attributeId);
        }
        if (method == PredictionSchemeMethod.None)
        {
            return null;
        }
        if (connectivityEncoder.GeometryType == Constants.EncodingType.TriangularMesh)
        {
            var meshPredictionScheme = CreateMeshPredictionScheme<TDataType, TTransform>((MeshEncoder)connectivityEncoder, method, attributeId, transform);

            if (meshPredictionScheme != null)
            {
                return meshPredictionScheme;
            }
        }
        return new PredictionSchemeDeltaEncoder<TDataType, TTransform>(connectivityEncoder.PointCloud!.GetAttributeById(attributeId)!, transform);
    }

    public static IPredictionSchemeEncoder<TDataType>? CreateMeshPredictionScheme<TDataType, TTransform>(MeshEncoder meshEncoder, PredictionSchemeMethod method, int attributeId, TTransform transform)
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
        where TTransform : IPredictionSchemeEncodingTransform<TDataType, TDataType>
    {
        var attribute = meshEncoder.PointCloud!.GetAttributeById(attributeId);

        if (method > PredictionSchemeMethod.Difference && method < PredictionSchemeMethod.Count)
        {
            var cornerTable = meshEncoder.CornerTable;
            var encodingData = meshEncoder.GetAttributeEncodingData(attributeId);

            if (cornerTable == null || encodingData == null)
            {
                return null;
            }
            var attributeCornerTable = meshEncoder.GetAttributeCornerTable(attributeId);
            MeshPredictionSchemeData meshPredictionSchemeData = attributeCornerTable == null
                ? new(meshEncoder.Mesh, cornerTable, encodingData.EncodedAttributeValueIndexToCornerMap, encodingData.VertexToEncodedAttributeValueIndexMap)
                : new(meshEncoder.Mesh, attributeCornerTable, encodingData.EncodedAttributeValueIndexToCornerMap, encodingData.VertexToEncodedAttributeValueIndexMap);
            return method switch
            {
                PredictionSchemeMethod.Parallelogram => new MeshPredictionSchemeParallelogramEncoder<TDataType, TTransform>(attribute, transform, meshPredictionSchemeData),
                PredictionSchemeMethod.ConstrainedMultiParallelogram => new MeshPredictionSchemeConstrainedMultiParallelogramEncoder<TDataType, TTransform>(attribute, transform, meshPredictionSchemeData),
                PredictionSchemeMethod.TexCoordsPortable => new MeshPredictionSchemeTexCoordsPortableEncoder<TDataType, TTransform>(attribute, transform, meshPredictionSchemeData),
                PredictionSchemeMethod.GeometricNormal => new MeshPredictionSchemeGeometricNormalEncoder<TDataType, TTransform>(attribute, transform, meshPredictionSchemeData),
                _ => null
            };
        }
        return null;
    }
}
