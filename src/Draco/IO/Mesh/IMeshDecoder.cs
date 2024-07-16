namespace Draco.IO.Mesh;

/// <summary>
/// represents a class that reconstructs a 3D mesh from input data that was encoded by a <see cref="IMeshEncoder"/>.
/// </summary>
internal interface IMeshDecoder : IConnectivityDecoder
{
    public Mesh? Mesh { get; }
}
