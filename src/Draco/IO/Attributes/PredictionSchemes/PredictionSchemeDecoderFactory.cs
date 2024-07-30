using System.Numerics;
using Draco.IO.Mesh;

namespace Draco.IO.Attributes.PredictionSchemes;

internal static class PredictionSchemeDecoderFactory
{
    public static IPredictionSchemeDecoder<TDataType, TTransform>? CreatePredictionSchemeForDecoder<TDataType, TTransform>(PredictionSchemeMethod method, int attributeId, ConnectivityDecoder connectivityDecoder, TTransform transform)
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
        where TTransform : PredictionSchemeDecodingTransform<TDataType>
    {
        if (method == PredictionSchemeMethod.None)
        {
            return null;
        }
        if (connectivityDecoder.GeometryType == Constants.EncodingType.TriangularMesh)
        {
            var meshPredictionScheme = CreateMeshPredictionScheme<TDataType, TTransform>((MeshDecoder)connectivityDecoder, method, attributeId, transform);

            if (meshPredictionScheme != null)
            {
                return meshPredictionScheme;
            }
        }
        return (IPredictionSchemeDecoder<TDataType, TTransform>)new PredictionSchemeDeltaDecoder<TDataType, TTransform>(connectivityDecoder.PointCloud!.GetAttributeById(attributeId)!, transform);
    }

    public static IPredictionSchemeDecoder<TDataType, TTransform>? CreateMeshPredictionScheme<TDataType, TTransform>(MeshDecoder meshDecoder, PredictionSchemeMethod method, int attributeId, TTransform transform)
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
        where TTransform : PredictionSchemeDecodingTransform<TDataType>
    {
        var attribute = meshDecoder.PointCloud!.GetAttributeById(attributeId)!;
        var cornerTable = meshDecoder.CornerTable;
        var encodingData = meshDecoder.GetAttributeEncodingData(attributeId);

        if (cornerTable == null || encodingData == null)
        {
            return null;
        }
        var attributeCornerTable = meshDecoder.GetAttributeCornerTable(attributeId);
        MeshPredictionSchemeData meshPredictionSchemeData = attributeCornerTable == null
            ? new(meshDecoder.Mesh, cornerTable, encodingData.EncodedAttributeValueIndexToCornerMap, encodingData.VertexToEncodedAttributeValueIndexMap)
            : new(meshDecoder.Mesh, attributeCornerTable, encodingData.EncodedAttributeValueIndexToCornerMap, encodingData.VertexToEncodedAttributeValueIndexMap);
        return method switch
        {
            PredictionSchemeMethod.Parallelogram => (IPredictionSchemeDecoder<TDataType, TTransform>)new MeshPredictionSchemeParallelogramDecoder<TDataType, TTransform>(attribute, transform, meshPredictionSchemeData),
            PredictionSchemeMethod.MultiParallelogram => (IPredictionSchemeDecoder<TDataType, TTransform>)new MeshPredictionSchemeMultiParallelogramDecoder<TDataType, TTransform>(attribute, transform, meshPredictionSchemeData),
            PredictionSchemeMethod.ConstrainedMultiParallelogram => (IPredictionSchemeDecoder<TDataType, TTransform>)new MeshPredictionSchemeConstrainedMultiParallelogramDecoder<TDataType, TTransform>(attribute, transform, meshPredictionSchemeData),
            // PredictionSchemeMethod.TexCoordsDeprecated => (IPredictionSchemeDecoder<TDataType, TTransform>)new MeshPredictionSchemeTexCoordsDecoder<TDataType, TTransform>(attribute, transform, meshPredictionSchemeData),
            // PredictionSchemeMethod.TexCoordsPortable => (IPredictionSchemeDecoder<TDataType, TTransform>)new MeshPredictionSchemeTexCoordsPortableDecoder<TDataType, TTransform>(attribute, transform, meshPredictionSchemeData),
            PredictionSchemeMethod.GeometricNormal => (IPredictionSchemeDecoder<TDataType, TTransform>)new MeshPredictionSchemeGeometricNormalDecoder<TDataType, TTransform>(attribute, transform, meshPredictionSchemeData),
            _ => null,
        };
    }
}
