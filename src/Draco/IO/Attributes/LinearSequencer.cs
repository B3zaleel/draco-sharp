namespace Draco.IO.Attributes;

internal class LinearSequencer(int pointsCount) : PointsSequencer
{
    private readonly int _pointsCount = pointsCount;

    protected override void GenerateSequenceInternal()
    {
        for (uint i = 0; i < _pointsCount; ++i)
        {
            PointIds.Add(i);
        }
    }

    public override void UpdatePointToAttributeIndexMapping(PointAttribute pointAttribute)
    {
        pointAttribute.SetIdentityMapping();
    }
}
