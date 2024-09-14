using Draco.IO.Attributes;
using Draco.IO.Enums;
using Draco.IO.Extensions;
using Draco.IO.Mesh.Traverser;

namespace Draco.IO.Mesh;

internal abstract class MeshEdgeBreakerEncoder : MeshEncoder
{
    private readonly List<uint> _cornerTraversalStack = [];
    private readonly List<bool> _visitedFaces = [];
    private MeshAttributeIndicesEncodingData? _posEncodingData;
    private MeshTraversalMethod? _posTraversalMethod;
    private List<uint> _processedConnectivityCorners = [];
    private readonly List<bool> _visitedVertexIds = [];
    private readonly List<int> _vertexTraversalLength = [];
    private readonly List<TopologySplitEventData> _topologySplitEventData = [];
    private readonly Dictionary<int, int> _faceToSplitSymbolMap = [];
    private readonly List<bool> _visitedHoles = [];
    private readonly List<int> _vertexHoleId = [];
    private int _lastEncodedSymbolId = -1;
    private uint _numSplitSymbols = 0;
    private readonly List<EncoderAttributeData> _attributeData = [];
    private readonly List<int> _attributeEncoderToDataIdMap = [];
    private readonly bool _useSingleConnectivity = false;
    public abstract int NumEncodedSymbols { get; }
    protected EncoderBuffer TraversalBuffer { get; private set; }

    public MeshEdgeBreakerEncoder(Config config, Mesh mesh) : base(config, mesh)
    {
        _attributeEncoderToDataIdMap.Clear();
        _useSingleConnectivity = config.IsOptionSet(ConfigOptionName.SplitMeshOnSeams)
            ? config.GetOption(ConfigOptionName.SplitMeshOnSeams, false)
            : config.Speed >= 6;
        TraversalBuffer = new EncoderBuffer(new BinaryWriter(new MemoryStream()));
    }

    public override void EncodeConnectivity(EncoderBuffer encoderBuffer)
    {
        CornerTable = _useSingleConnectivity ? CornerTable.CreateFromAllAttributes(Mesh) : CornerTable.CreateFromPositionAttribute(Mesh);
        if (CornerTable == null || CornerTable.FacesCount == CornerTable.DegeneratedFacesCount)
        {
            Assertions.Throw("All triangles are degenerate");
        }
        Traversal_Init();
        encoderBuffer.EncodeVarIntUnsigned((ulong)(CornerTable!.VerticesCount - CornerTable.IsolatedVerticesCount));
        encoderBuffer.EncodeVarIntUnsigned((ulong)(CornerTable.FacesCount - CornerTable.DegeneratedFacesCount));
        _visitedFaces.Fill(Mesh.FacesCount, false);
        _posEncodingData = new MeshAttributeIndicesEncodingData(0)
        {
            NumValues = 0,
            EncodedAttributeValueIndexToCornerMap = new List<uint>(CornerTable.FacesCount * 3)
        };
        _posEncodingData.VertexToEncodedAttributeValueIndexMap.Fill(CornerTable.VerticesCount, -1);
        _visitedVertexIds.Fill(CornerTable.VerticesCount, false);
        _vertexTraversalLength.Clear();
        _lastEncodedSymbolId = -1;
        _numSplitSymbols = 0;
        _topologySplitEventData.Clear();
        _faceToSplitSymbolMap.Clear();
        _visitedHoles.Clear();
        _vertexHoleId.Fill(CornerTable.VerticesCount, -1);
        _processedConnectivityCorners = new List<uint>(CornerTable.FacesCount);

        FindHoles();
        InitAttributeData();
        encoderBuffer.WriteByte((byte)_attributeData.Count);
        Traversal_SetNumAttributeData((uint)_attributeData.Count);
        Traversal_Start();

        var initFaceConnectivityCorners = new List<uint>();

        for (uint cornerId = 0; cornerId < CornerTable.CornersCount; ++cornerId)
        {
            var faceId = CornerTable.Face(cornerId);
            if (_visitedFaces[(int)faceId] || CornerTable.IsDegenerated(faceId))
            {
                continue;
            }
            var isInteriorConfig = TryFindInitFaceConfiguration(faceId, out uint startCorner);
            EncodeStartFaceConfiguration(encoderBuffer, isInteriorConfig);

            if (isInteriorConfig)
            {
                var vertexId = CornerTable.Vertex(startCorner);
                var nextVertexId = CornerTable.Vertex(CornerTable.Next(startCorner));
                var previousVertexId = CornerTable.Vertex(CornerTable.Previous(startCorner));
                _visitedVertexIds[(int)vertexId] = true;
                _visitedVertexIds[(int)nextVertexId] = true;
                _visitedVertexIds[(int)previousVertexId] = true;
                _vertexTraversalLength.Add(1);
                _visitedFaces[(int)faceId] = true;
                initFaceConnectivityCorners.Add(CornerTable.Next(startCorner));
                var oppositeId = CornerTable.Opposite(CornerTable.Next(startCorner));
                var oppositeFaceId = CornerTable.Face(oppositeId);

                if (oppositeFaceId != Constants.kInvalidFaceIndex && !_visitedFaces[(int)oppositeFaceId])
                {
                    EncodeConnectivityFromCorner(oppositeId);
                }
            }
            else
            {
                EncodeHole(CornerTable.Next(startCorner), true);
                EncodeConnectivityFromCorner(startCorner);
            }
        }
        _processedConnectivityCorners.Reverse();
        _processedConnectivityCorners.AddRange(initFaceConnectivityCorners);

        if (_attributeData.Count > 0)
        {
            _visitedFaces.Fill(Mesh.FacesCount, false);
            foreach (var cornerIndex in _processedConnectivityCorners)
            {
                EncodeAttributeConnectivitiesOnFace(cornerIndex);
            }
        }
        Traversal_Done();
        encoderBuffer.EncodeVarInt(NumEncodedSymbols);
        encoderBuffer.EncodeVarInt(_numSplitSymbols);
        EncodeSplitData(encoderBuffer);
        encoderBuffer.WriteBytes(TraversalBuffer.Data);
    }

    private void EncodeSplitData(EncoderBuffer encoderBuffer)
    {
        var numEvents = _topologySplitEventData.Count;
        encoderBuffer.EncodeVarIntUnsigned((ulong)numEvents);

        if (numEvents > 0)
        {
            int lastSourceSymbolId = 0;
            for (uint i = 0; i < numEvents; ++i)
            {
                var eventData = _topologySplitEventData[(int)i];
                encoderBuffer.EncodeVarIntUnsigned((ulong)(eventData.SourceSymbolId - lastSourceSymbolId));
                encoderBuffer.EncodeVarIntUnsigned(eventData.SourceSymbolId - eventData.SplitSymbolId);
                lastSourceSymbolId = (int)eventData.SourceSymbolId;
            }
            encoderBuffer.StartBitEncoding(false, (ulong)numEvents);
            for (uint i = 0; i < numEvents; ++i)
            {
                encoderBuffer.EncodeLeastSignificantBits32(1, _topologySplitEventData[(int)i].SourceEdge);
            }
            encoderBuffer.EndBitEncoding();
        }
    }

    private bool TryFindInitFaceConfiguration(uint faceId, out uint corner)
    {
        corner = 3 * faceId;

        for (byte i = 0; i < 3; ++i)
        {
            if (CornerTable!.Opposite(corner) == Constants.kInvalidCornerIndex)
            {
                return false;
            }
            if (_vertexHoleId[(int)CornerTable.Vertex(corner)] != -1)
            {
                var rightCorner = corner;
                while (rightCorner != Constants.kInvalidCornerIndex)
                {
                    corner = rightCorner;
                    rightCorner = CornerTable.SwingRight(rightCorner);
                }
                corner = CornerTable.Previous(corner);
                return false;
            }
            corner = CornerTable.Next(corner);
        }
        return true;
    }

    private void EncodeConnectivityFromCorner(uint cornerId)
    {
        _cornerTraversalStack.Clear();
        _cornerTraversalStack.Add(cornerId);

        while (_cornerTraversalStack.Count > 0)
        {
            cornerId = _cornerTraversalStack.Last();
            if (cornerId == Constants.kInvalidCornerIndex || _visitedFaces[(int)CornerTable!.Face(cornerId)])
            {
                _cornerTraversalStack.PopBack();
                continue;
            }
            int numVisitedFaces = 0;
            while (numVisitedFaces < Mesh.FacesCount)
            {
                ++numVisitedFaces;
                ++_lastEncodedSymbolId;
                var faceId = CornerTable.Face(cornerId);
                _visitedFaces[(int)faceId] = true;
                _processedConnectivityCorners.Add(cornerId);
                NewCornerReached(cornerId);
                var vertexId = CornerTable.Vertex(cornerId);
                var isOnBoundary = _vertexHoleId[(int)vertexId] != -1;

                if (!IsVertexVisited(vertexId))
                {
                    _visitedVertexIds[(int)vertexId] = true;
                    if (!isOnBoundary)
                    {
                        EncodeSymbol(Constants.EdgeBreakerTopologyBitPattern.C);
                        cornerId = CornerTable.GetRightCorner(cornerId);
                        continue;
                    }
                }
                var rightCornerId = CornerTable.GetRightCorner(cornerId);
                var leftCornerId = CornerTable.GetLeftCorner(cornerId);
                var rightFaceId = CornerTable.Face(rightCornerId);
                var leftFaceId = CornerTable.Face(leftCornerId);

                if (IsRightFaceVisited(cornerId))
                {
                    if (rightFaceId != Constants.kInvalidFaceIndex)
                    {
                        CheckAndStoreTopologySplitEvent(_lastEncodedSymbolId, (int)faceId, Constants.EdgeFaceName.RightFaceEdge, (int)rightFaceId);
                    }
                    if (IsLeftFaceVisited(cornerId))
                    {
                        if (leftFaceId != Constants.kInvalidFaceIndex)
                        {
                            CheckAndStoreTopologySplitEvent(_lastEncodedSymbolId, (int)faceId, Constants.EdgeFaceName.LeftFaceEdge, (int)leftFaceId);
                        }
                        EncodeSymbol(Constants.EdgeBreakerTopologyBitPattern.E);
                        _cornerTraversalStack.PopBack();
                        break;
                    }
                    else
                    {
                        EncodeSymbol(Constants.EdgeBreakerTopologyBitPattern.R);
                        cornerId = leftCornerId;
                    }
                }
                else
                {
                    if (IsLeftFaceVisited(cornerId))
                    {
                        if (leftFaceId != Constants.kInvalidFaceIndex)
                        {
                            CheckAndStoreTopologySplitEvent(_lastEncodedSymbolId, (int)faceId, Constants.EdgeFaceName.LeftFaceEdge, (int)leftFaceId);
                        }
                        EncodeSymbol(Constants.EdgeBreakerTopologyBitPattern.L);
                        cornerId = rightCornerId;
                    }
                    else
                    {
                        EncodeSymbol(Constants.EdgeBreakerTopologyBitPattern.S);
                        ++_numSplitSymbols;
                        if (isOnBoundary)
                        {
                            var holeId = _vertexHoleId[(int)vertexId];
                            if (!_visitedHoles[holeId])
                            {
                                EncodeHole(cornerId, false);
                            }
                        }
                        _faceToSplitSymbolMap[(int)faceId] = _lastEncodedSymbolId;
                        _cornerTraversalStack[_cornerTraversalStack.Count - 1] = leftCornerId;
                        _cornerTraversalStack.Add(rightCornerId);
                        break;
                    }
                }
            }
        }
    }

    private int EncodeHole(uint startCornerId, bool encodeFirstVertex)
    {
        var cornerId = CornerTable!.Previous(startCornerId);
        while (CornerTable.Opposite(cornerId) != Constants.kInvalidCornerIndex)
        {
            cornerId = CornerTable.Next(CornerTable.Opposite(cornerId));
        }
        var startVertexId = CornerTable.Vertex(startCornerId);
        int numEncodedHoleVertices = 0;
        if (encodeFirstVertex)
        {
            _visitedVertexIds[(int)startVertexId] = true;
            ++numEncodedHoleVertices;
        }
        _visitedHoles[_vertexHoleId[(int)startVertexId]] = true;
        var edgeStartVertexId = CornerTable.Vertex(CornerTable.Next(cornerId));
        var actVertexId = CornerTable.Vertex(CornerTable.Previous(cornerId));

        while (actVertexId != startVertexId)
        {
            edgeStartVertexId = actVertexId;
            _visitedVertexIds[(int)actVertexId] = true;
            ++numEncodedHoleVertices;
            cornerId = CornerTable.Next(cornerId);

            while (CornerTable.Opposite(cornerId) != Constants.kInvalidCornerIndex)
            {
                cornerId = CornerTable.Next(CornerTable.Opposite(cornerId));
            }
            actVertexId = CornerTable.Vertex(CornerTable.Previous(cornerId));
        }
        return numEncodedHoleVertices;
    }

    private bool IsVertexVisited(uint vertexId)
    {
        return _visitedVertexIds[(int)vertexId];
    }

    private bool IsRightFaceVisited(uint cornerId)
    {
        var nextCornerId = CornerTable!.Next(cornerId);
        var oppositeCornerId = CornerTable.Opposite(nextCornerId);

        return oppositeCornerId == Constants.kInvalidCornerIndex || _visitedFaces[(int)CornerTable.Face(oppositeCornerId)];
    }

    private bool IsLeftFaceVisited(uint cornerId)
    {
        var previousCornerId = CornerTable!.Previous(cornerId);
        var oppositeCornerId = CornerTable.Opposite(previousCornerId);

        return oppositeCornerId == Constants.kInvalidCornerIndex || _visitedFaces[(int)CornerTable.Face(oppositeCornerId)];
    }

    public bool IsFaceEncoded(uint faceId)
    {
        return _visitedFaces[(int)faceId];
    }

    private void FindHoles()
    {
        for (uint i = 0; i < CornerTable!.CornersCount; ++i)
        {
            if (CornerTable.IsDegenerated(CornerTable.Face(i)))
            {
                continue;
            }
            if (CornerTable.Opposite(i) == Constants.kInvalidCornerIndex)
            {
                var boundaryVertexId = CornerTable.Vertex(CornerTable.Next(i));
                if (_vertexHoleId[(int)boundaryVertexId] != -1)
                {
                    continue;
                }
                var boundaryId = _visitedHoles.Count;
                _visitedHoles.Add(false);
                var cornerId = i;
                while (_vertexHoleId[(int)boundaryVertexId] == -1)
                {
                    _vertexHoleId[(int)boundaryVertexId] = boundaryId;
                    cornerId = CornerTable.Next(cornerId);
                    while (CornerTable.Opposite(cornerId) != Constants.kInvalidCornerIndex)
                    {
                        cornerId = CornerTable.Next(CornerTable.Opposite(cornerId));
                    }
                    boundaryVertexId = CornerTable.Vertex(CornerTable.Next(cornerId));
                }
            }
        }
    }

    private int GetSplitSymbolIdOnFace(int faceId)
    {
        var lastItem = _faceToSplitSymbolMap.Last();

        if (_faceToSplitSymbolMap.TryGetValue(faceId, out int symbolId) && lastItem.Key != faceId && lastItem.Value != symbolId)
        {
            return symbolId;
        }
        return -1;
    }

    private void CheckAndStoreTopologySplitEvent(int srcSymbolId, int srcFaceId, byte srcEdge, int neighborFaceId)
    {
        var symbolId = GetSplitSymbolIdOnFace(neighborFaceId);

        if (symbolId == -1)
        {
            return;
        }
        var eventData = new TopologySplitEventData()
        {
            SplitSymbolId = (uint)symbolId,
            SourceSymbolId = (uint)srcSymbolId,
            SourceEdge = srcEdge
        };
        _topologySplitEventData.Add(eventData);
    }

    private void InitAttributeData()
    {
        if (_useSingleConnectivity)
        {
            return;
        }
        int numAttributes = Mesh.Attributes.Count;
        _attributeData.Resize(numAttributes - 1, () => new());
        if (numAttributes == 1)
        {
            return;
        }
        int dataIndex = 0;
        for (int attIndex = 0; attIndex < numAttributes; ++attIndex)
        {
            if (Mesh.GetAttributeById(attIndex).AttributeType == GeometryAttributeType.Position)
            {
                continue;
            }
            _attributeData[dataIndex].AttributeIndex = attIndex;
            _attributeData[dataIndex].EncodingData!.EncodedAttributeValueIndexToCornerMap.Clear();
            _attributeData[dataIndex].EncodingData!.NumValues = 0;
            _attributeData[dataIndex].ConnectivityData = new(CornerTable!, Mesh, Mesh.GetAttributeById(attIndex));
            ++dataIndex;
        }
    }

    private void EncodeAttributeConnectivitiesOnFace(uint corner)
    {
        uint[] corners = [corner, CornerTable!.Next(corner), CornerTable.Previous(corner)];
        var srcFaceId = CornerTable.Face(corner);
        _visitedFaces[(int)srcFaceId] = true;
        for (byte c = 0; c < 3; ++c)
        {
            var oppositeCorner = CornerTable.Opposite(corners[c]);
            if (oppositeCorner == Constants.kInvalidCornerIndex)
            {
                continue;
            }
            var oppositeFaceId = CornerTable.Face(oppositeCorner);
            if (_visitedFaces[(int)oppositeFaceId])
            {
                continue;
            }
            for (int i = 0; i < _attributeData.Count; ++i)
            {
                EncodeAttributeSeam(i, _attributeData[i].ConnectivityData!.IsCornerOppositeToSeamEdge(corners[c]));
            }
        }
    }

    public override void EncodeAttributesEncoderIdentifier(EncoderBuffer encoderBuffer, int attributeEncoderId)
    {
        var attributeDataId = (sbyte)_attributeEncoderToDataIdMap[attributeEncoderId];
        encoderBuffer.WriteSByte(attributeDataId);
        var elementType = MeshAttributeElementType.VertexAttribute;
        MeshTraversalMethod traversalMethod;

        if (attributeDataId >= 0)
        {
            elementType = Mesh.GetAttributeElementType(_attributeData[attributeDataId].AttributeIndex);
            traversalMethod = _attributeData[attributeDataId].TraversalMethod;
        }
        else
        {
            traversalMethod = (MeshTraversalMethod)_posTraversalMethod!;
        }
        if (elementType == MeshAttributeElementType.VertexAttribute || (elementType == MeshAttributeElementType.CornerAttribute && _attributeData[attributeDataId].ConnectivityData!.NoInteriorSeams))
        {
            encoderBuffer.WriteByte((byte)MeshAttributeElementType.VertexAttribute);
        }
        else
        {
            encoderBuffer.WriteByte((byte)MeshAttributeElementType.CornerAttribute);
        }
        encoderBuffer.WriteByte((byte)traversalMethod);
    }

    public override MeshAttributeCornerTable? GetAttributeCornerTable(int attributeId)
    {
        for (uint i = 0; i < _attributeData.Count; ++i)
        {
            if (_attributeData[(int)i].AttributeIndex == attributeId)
            {
                return _attributeData[(int)i].IsConnectivityUsed ? _attributeData[(int)i].ConnectivityData : null;
            }
        }
        return null;
    }

    public override MeshAttributeIndicesEncodingData? GetAttributeEncodingData(int attributeId)
    {
        for (uint i = 0; i < _attributeData.Count; ++i)
        {
            if (_attributeData[(int)i].AttributeIndex == attributeId)
            {
                return _attributeData[(int)i].EncodingData;
            }
        }
        return _posEncodingData;
    }

    public override void GenerateAttributesEncoder(int attributeId)
    {
        if (_useSingleConnectivity && AttributesEncoders.Count > 0)
        {
            AttributesEncoders[0]!.AddAttributeId(attributeId);
            return;
        }
        var elementType = Mesh.GetAttributeElementType(attributeId);
        var attribute = PointCloud!.GetAttributeById(attributeId);
        int attributeDataId = -1;
        for (uint i = 0; i < _attributeData.Count; ++i)
        {
            if (_attributeData[(int)i].AttributeIndex == attributeId)
            {
                attributeDataId = (int)i;
                break;
            }
        }
        var traversalMethod = MeshTraversalMethod.DepthFirst;
        PointsSequencer? sequencer;

        if (_useSingleConnectivity || attribute.AttributeType == GeometryAttributeType.Position || elementType == MeshAttributeElementType.VertexAttribute || (elementType == MeshAttributeElementType.CornerAttribute && _attributeData[attributeDataId].ConnectivityData!.NoInteriorSeams))
        {
            MeshAttributeIndicesEncodingData encodingData;

            if (_useSingleConnectivity || attribute.AttributeType == GeometryAttributeType.Position)
            {
                encodingData = _posEncodingData!;
            }
            else
            {
                encodingData = _attributeData[attributeDataId].EncodingData!;
                encodingData.VertexToEncodedAttributeValueIndexMap.Fill(CornerTable!.VerticesCount, -1);
                _attributeData[attributeDataId].IsConnectivityUsed = false;
            }
            if (Config.Speed == 0 && attribute.AttributeType != GeometryAttributeType.Position)
            {
                traversalMethod = MeshTraversalMethod.PredictionDegree;
                if (_useSingleConnectivity && Mesh.Attributes.Count > 1)
                {
                    traversalMethod = MeshTraversalMethod.DepthFirst;
                }
            }
            Traverser.Traverser? attributeTraverser = null;
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
            var traversalSequencer = new MeshTraversalSequencer(Mesh, _attributeData[attributeDataId].EncodingData!);
            var attributeObserver = new MeshAttributeIndicesEncodingObserver(_attributeData[attributeDataId].ConnectivityData!, Mesh, _attributeData[attributeDataId].EncodingData!, traversalSequencer);
            var attributeTraverser = new DepthFirstTraverser(_attributeData[attributeDataId].ConnectivityData!, attributeObserver);
            traversalSequencer.CornerOrders = _processedConnectivityCorners;
            traversalSequencer.Traverser = attributeTraverser;
            sequencer = traversalSequencer;
        }
        Assertions.ThrowIf(sequencer == null, "Sequencer must be set.");
        if (attributeDataId == -1)
        {
            _posTraversalMethod = traversalMethod;
        }
        else
        {
            _attributeData[attributeDataId].TraversalMethod = traversalMethod;
        }
        _attributeEncoderToDataIdMap.Add(attributeDataId);
        AttributesEncoders.Add(new SequentialAttributeEncodersController(sequencer!, attributeId, this, PointCloud));
    }

    public override void ComputeNumberOfEncodedPoints()
    {
        if (CornerTable == null)
        {
            EncodedPointsCount = 0;
            return;
        }
        EncodedPointsCount = CornerTable.VerticesCount - CornerTable.IsolatedVerticesCount;

        if (Mesh.Attributes.Count > 1)
        {
            var attributeCornerTables = new List<MeshAttributeCornerTable>();
            for (int i = 0; i < Mesh.Attributes.Count; ++i)
            {
                if (Mesh.GetAttributeById(i).AttributeType == GeometryAttributeType.Position)
                {
                    continue;
                }
                var attributeCornerTable = GetAttributeCornerTable(i);
                if (attributeCornerTable != null)
                {
                    attributeCornerTables.Add(attributeCornerTable);
                }
            }

            for (uint vertexIndex = 0; vertexIndex < CornerTable.VerticesCount; ++vertexIndex)
            {
                if (CornerTable.IsVertexIsolated(vertexIndex))
                {
                    continue;
                }
                var firstCornerIndex = CornerTable.LeftMostCorner(vertexIndex);
                var firstPointIndex = Mesh.CornerToPointId(firstCornerIndex);
                var lastPointIndex = firstPointIndex;
                var lastCornerIndex = firstCornerIndex;
                var cornerIndex = CornerTable.SwingRight(firstCornerIndex);
                int numAttributeSeams = 0;

                while (cornerIndex != Constants.kInvalidCornerIndex)
                {
                    var pointIndex = Mesh.CornerToPointId(cornerIndex);
                    var seamFound = false;
                    if (pointIndex != lastPointIndex)
                    {
                        seamFound = true;
                        lastPointIndex = pointIndex;
                    }
                    else
                    {
                        for (int i = 0; i < attributeCornerTables.Count; ++i)
                        {
                            if (attributeCornerTables[i].Vertex(cornerIndex) != attributeCornerTables[i].Vertex(lastCornerIndex))
                            {
                                seamFound = true;
                                break;
                            }
                        }
                    }
                    if (seamFound)
                    {
                        ++numAttributeSeams;
                    }
                    if (cornerIndex == firstCornerIndex)
                    {
                        break;
                    }
                    lastCornerIndex = cornerIndex;
                    cornerIndex = CornerTable.SwingRight(cornerIndex);
                }

                if (!CornerTable.IsOnBoundary(vertexIndex) && numAttributeSeams > 0)
                {
                    EncodedPointsCount += numAttributeSeams - 1;
                }
                else
                {
                    EncodedPointsCount += numAttributeSeams;
                }
            }
        }
    }

    public override void ComputeNumberOfEncodedFaces()
    {
        if (CornerTable == null)
        {
            EncodedFacesCount = 0;
            return;
        }
        EncodedFacesCount = CornerTable.FacesCount - CornerTable.DegeneratedFacesCount;
    }

    protected abstract void Traversal_Init();
    protected abstract void Traversal_SetNumAttributeData(uint numData);
    protected abstract void Traversal_Start();
    protected abstract void Traversal_Done();
    protected abstract void EncodeSymbol(uint symbol);
    protected abstract void EncodeAttributeSeam(int attributeId, bool isSeam);
    protected abstract void EncodeStartFaceConfiguration(EncoderBuffer encoderBuffer, bool interior);
    protected virtual void NewCornerReached(uint corner) { }
    protected virtual void NewActiveCornerReached(uint corner) { }
}
