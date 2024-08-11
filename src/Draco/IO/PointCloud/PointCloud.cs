using Draco.IO.Attributes;
using Draco.IO.Extensions;

namespace Draco.IO.PointCloud;

public class PointCloud
{
    private readonly List<PointAttribute> _attributes = [];
    private readonly List<List<int>> _namedAttributeIndex = [];

    public int PointsCount { get; set; } = 0;
    public int AttributesCount { get => _attributes.Count; }

    public PointCloud()
    {
        _namedAttributeIndex.Fill((int)GeometryAttributeType.NamedAttributesCount, () => new());
    }

    public int NumNamedAttributes(GeometryAttributeType type)
    {
        if (type == GeometryAttributeType.Invalid || type >= GeometryAttributeType.NamedAttributesCount)
        {
            return 0;
        }
        return _namedAttributeIndex[(sbyte)type].Count;
    }

    public int GetNamedAttributeId(GeometryAttributeType type)
    {
        return GetNamedAttributeId(type, 0);
    }

    public int GetNamedAttributeId(GeometryAttributeType type, int i)
    {
        return NumNamedAttributes(type) <= i ? -1 : _namedAttributeIndex[(sbyte)type][i];
    }

    public PointAttribute? GetNamedAttribute(GeometryAttributeType type)
    {
        return GetNamedAttribute(type, 0);
    }

    public PointAttribute? GetNamedAttribute(GeometryAttributeType type, int i)
    {
        var attId = GetNamedAttributeId(type, i);
        return attId == -1 ? null : _attributes[attId];
    }

    public PointAttribute? GetNamedAttributeByUniqueId(GeometryAttributeType type, uint uniqueId)
    {
        for (int attId = 0; attId < _namedAttributeIndex[(sbyte)type].Count; ++attId)
        {
            if (_attributes[_namedAttributeIndex[(sbyte)type][attId]].UniqueId == uniqueId)
            {
                return _attributes[_namedAttributeIndex[(sbyte)type][attId]];
            }
        }
        return null;
    }

    public PointAttribute GetAttributeById(int id)
    {
        Assertions.ThrowIf(id < 0 || id >= _attributes.Count, "Invalid attribute id.");
        return _attributes[id];
    }

    public PointAttribute? GetAttributeByUniqueId(uint uniqueId)
    {
        var attId = GetAttributeIdByUniqueId(uniqueId);
        return attId == -1 ? null : _attributes[attId];
    }

    public int GetAttributeIdByUniqueId(uint uniqueId)
    {
        for (int attId = 0; attId < _attributes.Count; ++attId)
        {
            if (_attributes[attId].UniqueId == uniqueId)
            {
                return attId;
            }
        }
        return -1;
    }

    public int AddAttribute(PointAttribute pointAttribute)
    {
        SetAttribute(_attributes.Count, pointAttribute);
        return _attributes.Count - 1;
    }

    public int AddAttribute(GeometryAttribute att, bool identityMapping, int numAttributeValues)
    {
        var pointAttribute = CreateAttribute(att, identityMapping, numAttributeValues);
        return pointAttribute == null ? -1 : AddAttribute(pointAttribute);
    }

    public PointAttribute? CreateAttribute(GeometryAttribute att, bool identityMapping, int numAttributeValues)
    {
        if (att.AttributeType == GeometryAttributeType.Invalid)
        {
            return null;
        }
        var pointAttribute = new PointAttribute(att);

        if (identityMapping)
        {
            pointAttribute.SetIdentityMapping();
            numAttributeValues = Math.Max(PointsCount, numAttributeValues);
        }
        else
        {
            pointAttribute.SetExplicitMapping(PointsCount);
        }
        if (numAttributeValues > 0)
        {
            pointAttribute.Reset(numAttributeValues);
        }
        return pointAttribute;
    }

    public virtual void SetAttribute(int attId, PointAttribute pointAttribute)
    {
        if (_attributes.Count <= attId)
        {
            _attributes.Resize(attId + 1, () => new());
        }
        if (pointAttribute.AttributeType < GeometryAttributeType.NamedAttributesCount)
        {
            _namedAttributeIndex[(sbyte)pointAttribute.AttributeType].Add(attId);
        }
        pointAttribute.UniqueId = (uint)attId;
        _attributes[attId] = pointAttribute;
    }
}
