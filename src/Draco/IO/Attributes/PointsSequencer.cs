namespace Draco.IO.Attributes;

internal abstract class PointsSequencer
{
    public List<uint> PointIds { get; private set; } = [];

    public virtual void GenerateSequence(List<uint> pointIds)
    {
        PointIds = pointIds;
        GenerateSequenceInternal();
    }

    public void AddPointId(uint pointId)
    {
        PointIds.Add(pointId);
    }

    protected abstract void GenerateSequenceInternal();
    public abstract void UpdatePointToAttributeIndexMapping(PointAttribute pointAttribute);
}
