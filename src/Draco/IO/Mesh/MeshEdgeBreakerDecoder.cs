using Draco.IO.Attributes;
using Draco.IO.Extensions;
using Draco.IO.Mesh.Traverser;

namespace Draco.IO.Mesh;

internal abstract class MeshEdgeBreakerDecoder : MeshDecoder
{
    private readonly List<int> _vertexTraversalLength = [];
    private readonly List<TopologySplitEventData> _topologySplitData = [];
    private readonly List<HoleEventData> _holeEventData = [];
    private readonly List<bool> _initFaceConfigurations = [];
    private readonly List<int> _initCorners = [];
    private readonly List<bool> _isVertHole = [];
    private uint _numNewVertices;
    private readonly Dictionary<int, int> _newToParentVertexMap = [];
    private uint _numEncodedVertices;
    private readonly List<int> _processedCornerIds = [];
    private readonly List<int> _processedConnectivityCorners = [];
    private MeshAttributeIndicesEncodingData? _posEncodingData;
    private int _posDataDecoderId = -1;
    private readonly List<DecoderAttributeData> _attributeData = [];

    public override void DecodeConnectivity(DecoderBuffer decoderBuffer)
    {
        _numNewVertices = 0;
        _newToParentVertexMap.Clear();
        if (decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 2))
        {
            _numNewVertices = decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 0)
                ? decoderBuffer.ReadUInt32()
                : (uint)decoderBuffer.DecodeVarIntUnsigned();
        }
        _numEncodedVertices = decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 0)
            ? decoderBuffer.ReadUInt32()
            : (uint)decoderBuffer.DecodeVarIntUnsigned();
        uint numFaces = decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 0)
            ? decoderBuffer.ReadUInt32()
            : (uint)decoderBuffer.DecodeVarIntUnsigned();
        Assertions.ThrowIf(_numEncodedVertices > numFaces * 3, "There cannot be more vertices than 3 * num_faces.");
        uint minNumFaceEdges = 3 * numFaces / 2;
        ulong numEncodedVertices64 = _numEncodedVertices;
        ulong maxNumVertexEdges = numEncodedVertices64 * (numEncodedVertices64 - 1) / 2;
        Assertions.ThrowIf(maxNumVertexEdges < minNumFaceEdges, "It is impossible to construct a manifold mesh with these properties.");
        byte numAttributeData = decoderBuffer.ReadByte();
        uint numEncodedSymbols = decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 0)
            ? decoderBuffer.ReadUInt32()
            : (uint)decoderBuffer.DecodeVarIntUnsigned();
        Assertions.ThrowIf(numFaces < numEncodedSymbols, "Number of faces needs to be the same or greater than the number of symbols.");
        uint maxEncodedFaces = numEncodedSymbols + (numEncodedSymbols / 3);
        Assertions.ThrowIf(numFaces > maxEncodedFaces, "Faces can only be 1 1/3 times bigger than number of encoded symbols.");
        uint numEncodedSplitSymbols = decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 0)
            ? decoderBuffer.ReadUInt32()
            : (uint)decoderBuffer.DecodeVarIntUnsigned();
        Assertions.ThrowIf(numEncodedSplitSymbols > numEncodedSymbols, "Split symbols are a sub-set of all symbols.");
        _vertexTraversalLength.Clear();
        CornerTable = new CornerTable();
        _processedCornerIds.Clear();
        _processedConnectivityCorners.Clear();
        _topologySplitData.Clear();
        _holeEventData.Clear();
        _initFaceConfigurations.Clear();
        _initCorners.Clear();
        _attributeData.Clear();
        _attributeData.Resize(numAttributeData, () => new());
        CornerTable.Reset((int)numFaces, (int)(_numEncodedVertices + numEncodedSplitSymbols));
        _isVertHole.Fill((int)(_numEncodedVertices + numEncodedSplitSymbols), true);
        int topologySplitDecodedBytes = -1;

        if (decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 2))
        {
            uint encodedConnectivitySize = decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 0)
                ? decoderBuffer.ReadUInt32()
                : (uint)decoderBuffer.DecodeVarIntUnsigned();
            Assertions.ThrowIf(encodedConnectivitySize == 0);
            var eventBuffer = new DecoderBuffer(decoderBuffer.ReadBytes((int)encodedConnectivitySize), decoderBuffer.BitStream_Version);
            topologySplitDecodedBytes = (int)encodedConnectivitySize;
            DecodeHoleAndTopologySplitEvents(eventBuffer);
        }
        else
        {
            DecodeHoleAndTopologySplitEvents(decoderBuffer);
        }
        Traversal_Init(decoderBuffer);
        Traversal_SetNumEncodedVertices(_numEncodedVertices + numEncodedSplitSymbols);
        Traversal_SetNumAttributeData(numAttributeData);
        Traversal_Start(decoderBuffer);

        int numConnectivityVertices = DecodeConnectivity(decoderBuffer, (int)numEncodedSymbols);
        Assertions.ThrowIf(numConnectivityVertices == -1);
        if (_attributeData.Count > 0)
        {
            // Decode connectivity of non-position attributes.
            if (decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 1))
            {
                for (uint ci = 0; ci < CornerTable.CornersCount; ci += 3)
                {
                    DecodeAttributeConnectivitiesOnFaceLegacy(ci);
                }
            }
            else
            {
                for (uint ci = 0; ci < CornerTable.CornersCount; ci += 3)
                {
                    DecodeAttributeConnectivitiesOnFace(ci);
                }
            }
        }
        Traversal_Done(decoderBuffer);

        // Decode attribute connectivity
        for (int i = 0; i < _attributeData.Count; ++i)
        {
            _attributeData[i].ConnectivityData = new(CornerTable);
            foreach (var c in _attributeData[i].AttributeSeamCorners)
            {
                _attributeData[i].ConnectivityData!.AddSeamEdge((uint)c);
            }
            _attributeData[i].ConnectivityData!.RecomputeVertices(null, null);
        }
        _posEncodingData = new(CornerTable.VerticesCount);
        for (int i = 0; i < _attributeData.Count; ++i)
        {
            int attConnectivityVertices = _attributeData[i].ConnectivityData!.VerticesCount;

            if (attConnectivityVertices < CornerTable.VerticesCount)
            {
                attConnectivityVertices = CornerTable.VerticesCount;
            }
            _attributeData[i].EncodingData = new(attConnectivityVertices);
        }
        AssignPointsToCorners(numConnectivityVertices);
    }

    private void DecodeHoleAndTopologySplitEvents(DecoderBuffer decoderBuffer)
    {
        uint numTopologySplits;
        if (decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 0))
        {
            numTopologySplits = decoderBuffer.ReadUInt32();
        }
        else
        {
            numTopologySplits = (uint)decoderBuffer.DecodeVarIntUnsigned();
        }
        if (numTopologySplits > 0)
        {
            Assertions.ThrowIf(numTopologySplits > CornerTable!.FacesCount);
            if (decoderBuffer.BitStream_Version < Constants.BitStreamVersion(1, 2))
            {
                for (uint i = 0; i < numTopologySplits; ++i)
                {
                    var event_data = new TopologySplitEventData
                    {
                        SplitSymbolId = decoderBuffer.ReadUInt32(),
                        SourceSymbolId = decoderBuffer.ReadUInt32()
                    };
                    byte edge_data = decoderBuffer.ReadByte();
                    event_data.SourceEdge = (uint)(edge_data & 1);
                    _topologySplitData.Add(event_data);
                }
            }
            else
            {
                int lastSourceSymbolId = 0;
                for (uint i = 0; i < numTopologySplits; ++i)
                {
                    var event_data = new TopologySplitEventData();
                    uint delta = (uint)decoderBuffer.DecodeVarIntUnsigned();
                    event_data.SourceSymbolId = (uint)(delta + lastSourceSymbolId);
                    delta = (uint)decoderBuffer.DecodeVarIntUnsigned();
                    Assertions.ThrowIf(delta > event_data.SourceSymbolId);
                    event_data.SplitSymbolId = (uint)(event_data.SourceSymbolId - (int)delta);
                    lastSourceSymbolId = (int)event_data.SourceSymbolId;
                    _topologySplitData.Add(event_data);
                }
                decoderBuffer.StartBitDecoding(false, out ulong _);
                for (uint i = 0; i < numTopologySplits; ++i)
                {
                    uint edge_data;
                    if (decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 2))
                    {
                        edge_data = decoderBuffer.DecodeLeastSignificantBits32(2);
                    }
                    else
                    {
                        edge_data = decoderBuffer.DecodeLeastSignificantBits32(1);
                    }
                    _topologySplitData[(int)i].SourceEdge = edge_data & 1;
                }
                decoderBuffer.EndBitDecoding();
            }
        }
        uint numHoleEvents = 0;
        if (decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 0))
        {
            numHoleEvents = decoderBuffer.ReadUInt32();
        }
        else if (decoderBuffer.BitStream_Version < Constants.BitStreamVersion(2, 1))
        {
            numHoleEvents = (uint)decoderBuffer.DecodeVarIntUnsigned();
        }
        if (numHoleEvents > 0)
        {
            if (decoderBuffer.BitStream_Version < Constants.BitStreamVersion(1, 2))
            {
                for (uint i = 0; i < numHoleEvents; ++i)
                {
                    var eventData = new HoleEventData
                    {
                        SymbolId = decoderBuffer.ReadInt32()
                    };
                    _holeEventData.Add(eventData);
                }
            }
            else
            {
                int lastSymbolId = 0;
                for (uint i = 0; i < numHoleEvents; ++i)
                {
                    var eventData = new HoleEventData();
                    uint delta = (uint)decoderBuffer.DecodeVarIntUnsigned();
                    eventData.SymbolId = (int)delta + lastSymbolId;
                    lastSymbolId = eventData.SymbolId;
                    _holeEventData.Add(eventData);
                }
            }
        }
    }

    private int DecodeConnectivity(DecoderBuffer decoderBuffer, int numSymbols)
    {
        List<uint> activeCornerStack = [];
        Dictionary<int, uint> topologySplitActiveCorners = [];
        List<uint> invalidVertices = [];
        var removeInvalidVertices = _attributeData.Count == 0;
        int maxNumVertices = _isVertHole.Count;
        int numFaces = 0;

        for (int symbolId = 0; symbolId < numSymbols; ++symbolId)
        {
            int face = numFaces++;
            bool checkTopologySplit = false;
            uint symbol = DecodeSymbol(decoderBuffer);

            if (symbol == Constants.EdgeBreakerTopologyBitPattern.C)
            {
                Assertions.ThrowIf(activeCornerStack.Count == 0);
                uint cornerA = activeCornerStack.Last();
                uint vertexX = CornerTable!.Vertex(CornerTable.Next(cornerA));
                uint cornerB = CornerTable.Next(CornerTable.LeftMostCorner(vertexX));
                Assertions.ThrowIf(cornerA == cornerB, "All matched corners must be different.");
                Assertions.ThrowIf(CornerTable.Opposite(cornerA) != Constants.kInvalidCornerIndex || CornerTable.Opposite(cornerB) != Constants.kInvalidCornerIndex, "All matched corners must be different.");
                uint corner = 3 * (uint)face;
                SetOppositeCorners(cornerA, corner + 1);
                SetOppositeCorners(cornerB, corner + 2);
                uint vertAPrev = CornerTable.Vertex(CornerTable.Previous(cornerA));
                uint vertBNext = CornerTable.Vertex(CornerTable.Next(cornerB));
                Assertions.ThrowIf(vertexX == vertAPrev || vertexX == vertBNext, "Encoding is invalid, because face vertices are degenerate.");
                CornerTable.MapCornerToVertex(corner, vertexX);
                CornerTable.MapCornerToVertex(corner + 1, vertBNext);
                CornerTable.MapCornerToVertex(corner + 2, vertAPrev);
                CornerTable.SetLeftMostCorner(vertAPrev, corner + 2);
                _isVertHole[(int)vertexX] = false;
                activeCornerStack[activeCornerStack.Count - 1] = corner;
            }
            else if (symbol == Constants.EdgeBreakerTopologyBitPattern.R || symbol == Constants.EdgeBreakerTopologyBitPattern.L)
            {
                Assertions.ThrowIf(activeCornerStack.Count == 0);
                uint cornerA = activeCornerStack.Last();
                Assertions.ThrowIf(CornerTable!.Opposite(cornerA) != Constants.kInvalidCornerIndex);
                uint corner = 3 * (uint)face;
                uint oppCorner, cornerL, cornerR;

                if (symbol == Constants.EdgeBreakerTopologyBitPattern.R)
                {
                    oppCorner = corner + 2;
                    cornerL = corner + 1;
                    cornerR = corner;
                }
                else
                {
                    oppCorner = corner + 1;
                    cornerL = corner;
                    cornerR = corner + 2;
                }
                SetOppositeCorners(oppCorner, cornerA);
                uint newVertIndex = CornerTable.AddNewVertex();
                Assertions.ThrowIf(CornerTable.VerticesCount > maxNumVertices, "Unexpected number of decoded vertices.");
                CornerTable.MapCornerToVertex(oppCorner, newVertIndex);
                CornerTable.SetLeftMostCorner(newVertIndex, oppCorner);
                uint vertexR = CornerTable.Vertex(CornerTable.Previous(cornerA));
                CornerTable.MapCornerToVertex(cornerR, vertexR);
                CornerTable.SetLeftMostCorner(vertexR, cornerR);
                CornerTable.MapCornerToVertex(cornerL, CornerTable.Vertex(CornerTable.Next(cornerA)));
                activeCornerStack[activeCornerStack.Count - 1] = corner;
                checkTopologySplit = true;
            }
            else if (symbol == Constants.EdgeBreakerTopologyBitPattern.S)
            {
                Assertions.ThrowIf(activeCornerStack.Count == 0);
                uint cornerB = activeCornerStack.Last();
                activeCornerStack.PopBack();
                var it = topologySplitActiveCorners.FirstOrDefault(symbols => symbols.Key == symbolId, new KeyValuePair<int, uint>(-1, 0));

                if (it.Key != -1)
                {
                    activeCornerStack.Add(it.Value);
                }
                Assertions.ThrowIf(activeCornerStack.Count == 0);
                var cornerA = activeCornerStack.Last();
                Assertions.ThrowIf(cornerA == cornerB, "All matched corners must be different.");
                Assertions.ThrowIf(CornerTable!.Opposite(cornerA) != Constants.kInvalidCornerIndex || CornerTable.Opposite(cornerB) != Constants.kInvalidCornerIndex);
                uint corner = 3 * (uint)face;
                SetOppositeCorners(cornerA, corner + 2);
                SetOppositeCorners(cornerB, corner + 1);
                var vertexP = CornerTable.Vertex(CornerTable.Previous(cornerA));
                CornerTable.MapCornerToVertex(corner, vertexP);
                CornerTable.MapCornerToVertex(corner + 1, CornerTable.Vertex(CornerTable.Next(cornerA)));
                var vertBPrev = CornerTable.Vertex(CornerTable.Previous(cornerB));
                CornerTable.MapCornerToVertex(corner + 2, vertBPrev);
                CornerTable.SetLeftMostCorner(vertBPrev, corner + 2);
                uint cornerN = CornerTable.Next(cornerB);
                uint vertexN = CornerTable.Vertex(cornerN);
                MergeVertices(vertexP, vertexN);
                CornerTable.SetLeftMostCorner(vertexP, CornerTable.LeftMostCorner(vertexN));
                uint firstCorner = cornerN;

                while (cornerN != Constants.kInvalidCornerIndex)
                {
                    CornerTable.MapCornerToVertex(cornerN, vertexP);
                    cornerN = CornerTable.SwingLeft(cornerN);
                    Assertions.ThrowIf(cornerN == firstCorner, "We reached the start again which should not happen for split symbols.");
                }
                CornerTable.MakeVertexIsolated(vertexN);

                if (removeInvalidVertices)
                {
                    invalidVertices.Add(vertexN);
                }
                activeCornerStack[activeCornerStack.Count - 1] = corner;
            }
            else if (symbol == Constants.EdgeBreakerTopologyBitPattern.E)
            {
                uint corner = 3 * (uint)face;
                uint firstVertIndex = CornerTable!.AddNewVertex();
                CornerTable.MapCornerToVertex(corner, firstVertIndex);
                CornerTable.MapCornerToVertex(corner + 1, CornerTable.AddNewVertex());
                CornerTable.MapCornerToVertex(corner + 2, CornerTable.AddNewVertex());
                Assertions.ThrowIf(CornerTable.VerticesCount > maxNumVertices, "Unexpected number of decoded vertices.");
                CornerTable.SetLeftMostCorner(firstVertIndex, corner);
                CornerTable.SetLeftMostCorner(firstVertIndex + 1, corner + 1);
                CornerTable.SetLeftMostCorner(firstVertIndex + 2, corner + 2);
                activeCornerStack.Add(corner);
                checkTopologySplit = true;
            }
            else
            {
                Assertions.Throw("Unknown symbol decoded.");
            }
            NewActiveCornerReached(activeCornerStack.Last());
            if (checkTopologySplit)
            {
                int encoderSymbolId = numSymbols - symbolId - 1;

                while (IsTopologySplit(encoderSymbolId, out int split_edge, out int encoderSplitSymbolId))
                {
                    Assertions.ThrowIf(encoderSplitSymbolId < 0, "Wrong split symbol id.");
                    uint actTopCorner = activeCornerStack.Last();
                    uint newActiveCorner = split_edge == Constants.EdgeFaceName.RightFaceEdge ? CornerTable!.Next(actTopCorner) : CornerTable!.Previous(actTopCorner);
                    int decoderSplitSymbolId = numSymbols - encoderSplitSymbolId - 1;
                    topologySplitActiveCorners[decoderSplitSymbolId] = newActiveCorner;
                }
            }
        }
        Assertions.ThrowIf(CornerTable!.VerticesCount > maxNumVertices, "Unexpected number of decoded vertices.");
        while (activeCornerStack.Count > 0)
        {
            var corner = activeCornerStack.Last();
            activeCornerStack.PopBack();
            var interiorFace = DecodeStartFaceConfiguration(decoderBuffer);
            if (interiorFace)
            {
                Assertions.ThrowIf(numFaces >= CornerTable.FacesCount, "More faces than expected added to the mesh.");
                var cornerA = corner;
                var vertexN = CornerTable.Vertex(CornerTable.Next(cornerA));
                var cornerB = CornerTable.Next(CornerTable.LeftMostCorner(vertexN));
                var vertexX = CornerTable.Vertex(CornerTable.Next(cornerB));
                var cornerC = CornerTable.Next(CornerTable.LeftMostCorner(vertexX));
                Assertions.ThrowIf(corner == cornerB || corner == cornerC || cornerB == cornerC, "All matched corners must be different.");
                Assertions.ThrowIf(CornerTable.Opposite(corner) != Constants.kInvalidCornerIndex || CornerTable.Opposite(cornerB) != Constants.kInvalidCornerIndex || CornerTable.Opposite(cornerC) != Constants.kInvalidCornerIndex, "One of the corners is already opposite to an existing face, which should not happen unless the input was tampered with.");
                var vertexP = CornerTable.Vertex(CornerTable.Next(cornerC));
                var face = numFaces++;
                uint newCorner = 3 * (uint)face;
                SetOppositeCorners(newCorner, corner);
                SetOppositeCorners(newCorner + 1, cornerB);
                SetOppositeCorners(newCorner + 2, cornerC);

                CornerTable.MapCornerToVertex(newCorner, vertexX);
                CornerTable.MapCornerToVertex(newCorner + 1, vertexP);
                CornerTable.MapCornerToVertex(newCorner + 2, vertexN);
                for (byte ci = 0; ci < 3; ++ci)
                {
                    _isVertHole[(int)CornerTable.Vertex(newCorner + ci)] = false;
                }
                _initFaceConfigurations.Add(true);
                _initCorners.Add((int)newCorner);
            }
            else
            {
                _initFaceConfigurations.Add(false);
                _initCorners.Add((int)corner);
            }
        }
        Assertions.ThrowIf(numFaces != CornerTable.FacesCount, "Unexpected number of decoded faces.");
        int numVertices = CornerTable.VerticesCount;

        foreach (var invalidVertex in invalidVertices)
        {
            uint srcVertex = (uint)numVertices - 1;
            while (CornerTable.LeftMostCorner(srcVertex) == Constants.kInvalidCornerIndex)
            {
                srcVertex = (uint)--numVertices - 1;
            }
            if (srcVertex < invalidVertex)
            {
                continue;
            }
            foreach (uint cornerId in new VertexCornersIterator(CornerTable, srcVertex, true))
            {
                Assertions.ThrowIf(CornerTable.Vertex(cornerId) != srcVertex, "Vertex mapped to |cornerId| was not |srcVertex|. This indicates corrupted data and we should terminate the decoding.");
                CornerTable.MapCornerToVertex(cornerId, invalidVertex);
            }
            CornerTable.SetLeftMostCorner(invalidVertex, CornerTable.LeftMostCorner(srcVertex));
            CornerTable.MakeVertexIsolated(srcVertex);
            _isVertHole[(int)invalidVertex] = _isVertHole[(int)srcVertex];
            _isVertHole[(int)srcVertex] = false;
            numVertices--;
        }
        return numVertices;
    }

    private void SetOppositeCorners(uint corner_0, uint corner_1)
    {
        CornerTable!.SetOppositeCorner(corner_0, corner_1);
        CornerTable.SetOppositeCorner(corner_1, corner_0);
    }

    private bool IsTopologySplit(int encoderSymbolId, out int faceEdge, out int encoderSplitSymbolId)
    {
        faceEdge = -1;
        encoderSplitSymbolId = -1;
        if (_topologySplitData.Count == 0)
        {
            return false;
        }
        if (_topologySplitData.Last().SourceSymbolId > encoderSymbolId)
        {
            encoderSplitSymbolId = -1;
            return true;
        }
        if (_topologySplitData.Last().SourceSymbolId != encoderSymbolId)
        {
            return false;
        }
        faceEdge = (int)_topologySplitData.Last().SourceEdge;
        encoderSplitSymbolId = (int)_topologySplitData.Last().SplitSymbolId;
        _topologySplitData.PopBack();
        return true;
    }

    private void DecodeAttributeConnectivitiesOnFaceLegacy(uint corner)
    {
        uint[] corners = [
            corner,
            CornerTable!.Next(corner),
            CornerTable.Previous(corner)
        ];
        for (byte c = 0; c < 3; ++c)
        {
            uint oppCorner = CornerTable.Opposite(corners[c]);
            if (oppCorner == Constants.kInvalidCornerIndex)
            {
                for (int i = 0; i < _attributeData.Count; ++i)
                {
                    _attributeData[i].AttributeSeamCorners.Add((int)corners[c]);
                }
                continue;
            }
            for (int i = 0; i < _attributeData.Count; ++i)
            {
                var isSeam = DecodeAttributeSeam(i);
                if (isSeam == 1U)
                {
                    _attributeData[i].AttributeSeamCorners.Add((int)corners[c]);
                }
            }
        }
    }

    private void DecodeAttributeConnectivitiesOnFace(uint corner)
    {
        uint[] corners = [
            corner,
            CornerTable!.Next(corner),
            CornerTable.Previous(corner)
        ];
        uint srcFaceId = CornerTable.Face(corner);
        for (byte c = 0; c < 3; ++c)
        {
            uint oppCorner = CornerTable.Opposite(corners[c]);
            if (oppCorner == Constants.kInvalidCornerIndex)
            {
                for (int i = 0; i < _attributeData.Count; ++i)
                {
                    _attributeData[i].AttributeSeamCorners.Add((int)corners[c]);
                }
            }
            uint oppFaceId = CornerTable.Face(oppCorner);
            if (oppFaceId < srcFaceId)
            {
                continue;
            }
            for (int i = 0; i < _attributeData.Count; ++i)
            {
                var isSeam = DecodeAttributeSeam(i);
                if (isSeam != 0)
                {
                    _attributeData[i].AttributeSeamCorners.Add((int)corners[c]);
                }
            }
        }
    }

    private void AssignPointsToCorners(int numConnectivityVertices)
    {
        Mesh!.SetNumFaces(CornerTable!.FacesCount);
        if (_attributeData.Count == 0)
        {
            for (uint f = 0; f < Mesh.FacesCount; ++f)
            {
                var face = new int[3];
                var startCorner = 3 * f;
                for (byte c = 0; c < 3; ++c)
                {
                    face[c] = (int)CornerTable.Vertex(startCorner + c);
                }
                Mesh.SetFace(f, face);
            }
            Mesh.PointsCount = numConnectivityVertices;
            return;
        }
        var pointToCornerMap = new List<int>();
        var cornerToPointMap = new List<int>();
        cornerToPointMap.Fill(CornerTable.CornersCount, 0);
        for (uint v = 0; v < CornerTable.VerticesCount; ++v)
        {
            var c = CornerTable.LeftMostCorner(v);
            if (c == Constants.kInvalidCornerIndex)
            {
                continue;
            }
            var deduplicationFirstCorner = c;
            if (_isVertHole[(int)v])
            {
                deduplicationFirstCorner = c;
            }
            else
            {
                for (int i = 0; i < _attributeData.Count; ++i)
                {
                    if (!_attributeData[i].ConnectivityData!.IsCornerOnSeam(c))
                    {
                        continue;
                    }
                    var vertId = _attributeData[i].ConnectivityData!.Vertex(c);
                    var actC = CornerTable.SwingRight(c);
                    var seamFound = false;
                    while (actC != c)
                    {
                        Assertions.ThrowIf(actC == Constants.kInvalidCornerIndex);
                        if (_attributeData[i].ConnectivityData!.Vertex(actC) != vertId)
                        {
                            deduplicationFirstCorner = actC;
                            seamFound = true;
                            break;
                        }
                        actC = CornerTable.SwingRight(actC);
                    }
                    if (seamFound)
                    {
                        break;
                    }
                }
            }
            c = deduplicationFirstCorner;
            cornerToPointMap[(int)c] = pointToCornerMap.Count;
            pointToCornerMap.Add((int)c);
            var prevC = c;
            c = CornerTable.SwingRight(c);
            while (c != Constants.kInvalidCornerIndex && c != deduplicationFirstCorner)
            {
                var attributeSeam = false;
                for (int i = 0; i < _attributeData.Count; ++i)
                {
                    if (_attributeData[i].ConnectivityData!.Vertex(c) != _attributeData[i].ConnectivityData!.Vertex(prevC))
                    {
                        attributeSeam = true;
                        break;
                    }
                }
                if (attributeSeam)
                {
                    cornerToPointMap[(int)c] = pointToCornerMap.Count;
                    pointToCornerMap.Add((int)c);
                }
                else
                {
                    cornerToPointMap[(int)c] = cornerToPointMap[(int)prevC];
                }
                prevC = c;
                c = CornerTable.SwingRight(c);
            }
        }
        // Add faces.
        for (uint f = 0; f < Mesh.FacesCount; ++f)
        {
            var face = new int[3];
            for (byte c = 0; c < 3; ++c)
            {
                face[c] = cornerToPointMap[(int)(3 * f + c)];
            }
            Mesh.SetFace(f, face);
        }
        Mesh.PointsCount = pointToCornerMap.Count;
    }

    protected override void CreateAttributesDecoder(DecoderBuffer decoderBuffer, int attDecoderId)
    {
        var attDataId = decoderBuffer.ReadSByte();
        var decoderType = decoderBuffer.ReadByte();

        if (attDataId >= 0)
        {
            Assertions.ThrowIf(attDataId >= _attributeData.Count, "Unexpected attribute data.");
            Assertions.ThrowIf(_attributeData[attDataId].DecoderId >= 0, "Ensure that the attribute data is not mapped to a different attributes decoder already.");
            _attributeData[attDataId].DecoderId = attDecoderId;
        }
        else
        {
            Assertions.ThrowIf(_posDataDecoderId >= 0, "Some other routine is already using the data.");
            _posDataDecoderId = attDecoderId;
        }
        var traversalMethod = MeshTraversalMethod.DepthFirst;
        if (decoderBuffer.BitStream_Version >= Constants.BitStreamVersion(1, 2))
        {
            var traversalMethodEncoded = decoderBuffer.ReadByte();
            Assertions.ThrowIf(traversalMethodEncoded >= (byte)MeshTraversalMethod.Count, "Traversal method is invalid.");
            traversalMethod = (MeshTraversalMethod)traversalMethodEncoded;
        }
        PointsSequencer? sequencer = null;
        if (decoderType == (byte)MeshAttributeElementType.VertexAttribute)
        {
            MeshAttributeIndicesEncodingData? encodingData;
            Traverser.Traverser? attributeTraverser = null;

            if (attDataId < 0)
            {
                encodingData = _posEncodingData;
            }
            else
            {
                encodingData = _attributeData[attDataId].EncodingData;
                _attributeData[attDataId].IsConnectivityUsed = false;
            }
            var traversalSequencer = new MeshTraversalSequencer(Mesh, encodingData!);
            var attributeObserver = new MeshAttributeIndicesEncodingObserver(CornerTable!, Mesh, encodingData!, traversalSequencer);

            if (traversalMethod == MeshTraversalMethod.PredictionDegree)
            {
                attributeTraverser = new MaxPredictionDegreeTraverser(CornerTable!, attributeObserver);
            }
            else if (traversalMethod == MeshTraversalMethod.DepthFirst)
            {
                attributeTraverser = new DepthFirstTraverser(CornerTable!, attributeObserver);
            }
            else
            {
                Assertions.Throw("Unsupported attribute traversal method.");
            }
            traversalSequencer.Traverser = attributeTraverser;
            sequencer = traversalSequencer;
        }
        else
        {
            Assertions.ThrowIf(traversalMethod != MeshTraversalMethod.DepthFirst, "Unsupported attribute traversal method.");
            Assertions.ThrowIf(attDataId < 0, "Attribute data must be specified.");
            var traversalSequencer = new MeshTraversalSequencer(Mesh, _attributeData[attDataId].EncodingData!);
            var attributeObserver = new MeshAttributeIndicesEncodingObserver(_attributeData[attDataId].ConnectivityData!, Mesh, _attributeData[attDataId].EncodingData!, traversalSequencer);
            var attributeTraverser = new DepthFirstTraverser(_attributeData[attDataId].ConnectivityData!, attributeObserver);
            traversalSequencer.Traverser = attributeTraverser;
            sequencer = traversalSequencer;
        }
        Assertions.ThrowIf(sequencer == null, "Sequencer must be set.");
        SetAttributesDecoder(attDecoderId, new SequentialAttributeDecodersController(sequencer!, this, Mesh));
    }

    public override MeshAttributeCornerTable? GetAttributeCornerTable(int attId)
    {
        for (uint i = 0; i < _attributeData.Count; ++i)
        {
            var decoderId = _attributeData[(int)i].DecoderId;

            if (decoderId < 0 || decoderId >= AttributesDecoders.Count)
            {
                continue;
            }
            var decoder = AttributesDecoders[decoderId];

            for (int j = 0; j < decoder!.AttributesCount; ++j)
            {
                if (decoder.GetAttributeId(j) == attId)
                {
                    return _attributeData[(int)i].IsConnectivityUsed ? _attributeData[(int)i].ConnectivityData : null;
                }
            }
        }
        return null;
    }

    public override MeshAttributeIndicesEncodingData? GetAttributeEncodingData(int attId)
    {
        if (attId < 0 || attId >= _attributeData.Count)
        {
            return null;
        }
        for (uint i = 0; i < _attributeData.Count; ++i)
        {
            var decoderId = _attributeData[(int)i].DecoderId;

            if (decoderId < 0 || decoderId >= AttributesDecoders.Count)
            {
                continue;
            }
            var decoder = AttributesDecoders[decoderId];

            for (int j = 0; j < decoder!.AttributesCount; ++j)
            {
                if (decoder.GetAttributeId(j) == attId)
                {
                    return _attributeData[(int)i].EncodingData;
                }
            }
        }
        return _posEncodingData;
    }

    protected abstract void Traversal_Init(DecoderBuffer decoderBuffer);
    protected abstract void Traversal_SetNumEncodedVertices(uint num_vertices);
    protected abstract void Traversal_SetNumAttributeData(uint num_data);
    protected abstract void Traversal_Start(DecoderBuffer decoderBuffer);
    protected abstract void Traversal_Done(DecoderBuffer decoderBuffer);
    protected abstract uint DecodeAttributeSeam(int attribute);
    protected abstract uint DecodeSymbol(DecoderBuffer decoderBuffer);
    protected virtual void MergeVertices(uint dest, uint source) { }
    protected virtual void NewActiveCornerReached(uint corner) { }
    protected abstract bool DecodeStartFaceConfiguration(DecoderBuffer decoderBuffer);
}
