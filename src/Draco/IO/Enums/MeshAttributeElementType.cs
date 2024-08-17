namespace Draco.IO.Enums;

/// <summary>
/// Represents different variants of <see cref="Mesh.Mesh"/> attributes.
/// </summary>
public enum MeshAttributeElementType : byte
{
    /// <summary>
    /// All corners attached to a vertex share the same attribute value. A typical example are the vertex positions and often vertex colors.
    /// </summary>
    VertexAttribute = 0,
    /// <summary>
    /// The most general attribute where every corner of the mesh can have a different attribute value. Often used for texture coordinates or normals.
    /// </summary>
    CornerAttribute = 1,
    /// <summary>
    /// All corners of a single face share the same value.
    /// </summary>
    FaceAttribute = 2
}
