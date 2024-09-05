using Draco.IO.Attributes;
using Draco.IO.Extensions;

namespace Draco.IO;

internal abstract class ConnectivityEncoder(Config config)
{
    public Config Config { get; } = config;
    public int BitStreamVersion { get; set; }
    public virtual int GeometryType { get; }
    public virtual PointCloud.PointCloud? PointCloud { get; }
    public List<AttributesEncoder> AttributesEncoders { get; } = [];
    public List<int> AttributeToEncoderMap { get; } = [];
    public List<int> AttributesEncoderIdsOrder { get; } = [];
    public int EncodedPointsCount { get; protected set; }

    public PointAttribute? GetPortableAttribute(int parentAttributeId)
    {
        if (parentAttributeId < 0 || parentAttributeId >= PointCloud!.Attributes.Count)
        {
            return null;
        }
        int parentAttributeEncoderId = AttributeToEncoderMap[parentAttributeId];
        return AttributesEncoders[parentAttributeEncoderId]!.GetPortableAttribute(parentAttributeId);
    }

    public void MarkParentAttribute(int parentAttributeId)
    {
        if (parentAttributeId < 0 || parentAttributeId >= PointCloud!.Attributes.Count)
        {
            return;
        }
        int parentAttributeEncoderId = AttributeToEncoderMap[parentAttributeId];
        AttributesEncoders[parentAttributeEncoderId]!.MarkParentAttribute(parentAttributeId);
    }

    public abstract void EncodeConnectivity(EncoderBuffer encoderBuffer);

    public void EncodeAttributes(EncoderBuffer encoderBuffer)
    {
        GenerateAttributesEncoders();
        encoderBuffer.WriteByte((byte)AttributesEncoders.Count);
        RearrangeAttributesEncoders();
        foreach (int attributeEncoderId in AttributesEncoderIdsOrder)
        {
            EncodeAttributesEncoderIdentifier(encoderBuffer, attributeEncoderId);
        }
        foreach (int attributeEncoderId in AttributesEncoderIdsOrder)
        {
            AttributesEncoders[attributeEncoderId]!.EncodeAttributesData(encoderBuffer);
        }
        foreach (int attributeEncoderId in AttributesEncoderIdsOrder)
        {
            AttributesEncoders[attributeEncoderId]!.EncodeAttributes(encoderBuffer);
        }
    }

    public void GenerateAttributesEncoders()
    {
        AttributesEncoders.Clear();
        for (int i = 0; i < PointCloud!.Attributes.Count; ++i)
        {
            GenerateAttributesEncoder(i);
        }
        AttributeToEncoderMap.Resize(PointCloud!.Attributes.Count, -1);
        for (uint i = 0; i < AttributesEncoders.Count; ++i)
        {
            for (uint j = 0; j < AttributesEncoders[(int)i].AttributesCount; ++j)
            {
                AttributeToEncoderMap[AttributesEncoders[(int)i].GetAttributeId((int)j)] = (int)i;
            }
        }
    }

    public void RearrangeAttributesEncoders()
    {
        List<bool> isEncoderProcessed = [];
        uint numProcessedEncoders = 0;
        isEncoderProcessed.Fill(AttributesEncoders.Count, false);

        while (numProcessedEncoders < AttributesEncoders.Count)
        {
            var encoderProcessed = false;

            for (uint i = 0; i < AttributesEncoders.Count; ++i)
            {
                if (isEncoderProcessed[(int)i])
                {
                    continue;
                }
                var canBeProcessed = true;
                for (uint p = 0; p < AttributesEncoders[(int)i]!.AttributesCount; ++p)
                {
                    var attributeId = AttributeToEncoderMap[AttributesEncoders[(int)i]!.GetAttributeId((int)p)];
                    for (int ap = 0; ap < AttributesEncoders[(int)i].NumParentAttributes(attributeId); ++ap)
                    {
                        var parentAttributeId = AttributesEncoders[(int)i]!.GetParentAttributeId(attributeId, ap);
                        var parentEncoderId = AttributeToEncoderMap[parentAttributeId];
                        if (parentAttributeId != i && !isEncoderProcessed[parentEncoderId])
                        {
                            canBeProcessed = false;
                            break;
                        }
                    }
                }
                if (!canBeProcessed)
                {
                    continue;
                }
                AttributesEncoderIdsOrder[(int)numProcessedEncoders++] = (int)i;
                isEncoderProcessed[(int)i] = true;
                encoderProcessed = true;
            }
            if (!encoderProcessed && numProcessedEncoders < AttributesEncoders.Count)
            {
                Assertions.Throw("No encoder was processed but there are still some remaining unprocessed encoders");
            }
        }
        List<int> attributeEncodingOrder = [];
        List<bool> isAttributeProcessed = [];
        isAttributeProcessed.Fill(PointCloud!.Attributes.Count, false);
        int numProcessedAttributes;
        for (uint attributeEncoderOrder = 0; attributeEncoderOrder < AttributesEncoderIdsOrder.Count; ++attributeEncoderOrder)
        {
            var attributeEncoderId = AttributesEncoderIdsOrder[(int)attributeEncoderOrder];
            var numEncoderAttributes = AttributesEncoders[attributeEncoderId]!.AttributesCount;
            if (numEncoderAttributes < 2)
            {
                continue;
            }
            numProcessedAttributes = 0;

            while (numProcessedAttributes < numEncoderAttributes)
            {
                var attributeProcessed = false;
                for (int i = 0; i < numEncoderAttributes; ++i)
                {
                    var attributeId = AttributesEncoders[attributeEncoderId]!.GetAttributeId(i);
                    if (isAttributeProcessed[attributeId])
                    {
                        continue;
                    }
                    var canBeProcessed = true;
                    for (int p = 0; p < AttributesEncoders[attributeEncoderId]!.NumParentAttributes(attributeId); ++p)
                    {
                        var parentAttributeId = AttributesEncoders[attributeEncoderId]!.GetParentAttributeId(attributeId, p);
                        if (!isAttributeProcessed[parentAttributeId])
                        {
                            canBeProcessed = false;
                            break;
                        }
                    }
                    if (!canBeProcessed)
                    {
                        continue;
                    }
                    attributeEncodingOrder[numProcessedAttributes++] = i;
                    isAttributeProcessed[i] = true;
                    attributeProcessed = true;
                }
                if (!attributeProcessed && numProcessedAttributes < numEncoderAttributes)
                {
                    Assertions.Throw("No attribute was processed but there are still some remaining unprocessed attributes");
                }
            }
            AttributesEncoders[attributeEncoderId].SetAttributeIds(attributeEncodingOrder);
        }
    }

    public virtual void EncodeAttributesEncoderIdentifier(EncoderBuffer encoderBuffer, int attributeEncoderId) { }
    public abstract void GenerateAttributesEncoder(int attributeEncoderId);
    public abstract void ComputeNumberOfEncodedPoints();
}
