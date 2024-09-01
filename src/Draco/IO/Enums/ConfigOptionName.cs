namespace Draco.IO.Enums;

public static class ConfigOptionName
{
    public const string EncodingSpeed = "encoding_speed";
    public const string DecodingSpeed = "decoding_speed";
    public const string CompressConnectivity = "compress_connectivity";
    public const string SplitMeshOnSeams = "split_mesh_on_seams";
    public const string UseBuiltInAttributeCompression = "use_built_in_attribute_compression";
    public const string EdgeBreakerMethod = "edgebreaker_method";
    public const string StoreNumberOfEncodedFaces = "store_number_of_encoded_faces";
    public const string StoreNumberOfEncodedPoints = "store_number_of_encoded_points";
    public const string SymbolEncodingMethod = "symbol_encoding_method";
    public const string SymbolEncodingCompressionLevel = "symbol_encoding_compression_level";

    public static class Attribute
    {
        public const string SkipAttributeTransform = "skip_attribute_transform";
        public const string QuantizationBits = "quantization_bits";
        public const string QuantizationOrigin = "quantization_origin";
        public const string QuantizationRange = "quantization_range";
        public const string PredictionScheme = "prediction_scheme";
    }

    public static class Feature
    {
        public const string EdgeBreaker = "standard_edgebreaker";
        public const string PredictiveEdgeBreaker = "predictive_edgebreaker";
    }
}
