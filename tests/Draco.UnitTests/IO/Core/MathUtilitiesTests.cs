using Draco.IO.Core;

namespace Draco.UnitTests.IO.Core;

public class MathUtilitiesTests
{
    [Theory]
    [InlineData(0L, 0L)]
    [InlineData(4L, 2L)]
    [InlineData(48722615824L, 220732L)]
    public void IntSqrt_GivenNumber_ReturnsExpectedValue(long number, long expectedResult)
    {
        // Arrange

        // Act
        var result = MathUtilities.IntSqrt(number);

        // Assert
        result.Should().Be(expectedResult);
    }
}
