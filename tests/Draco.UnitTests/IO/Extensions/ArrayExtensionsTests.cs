using Draco.IO.Extensions;

namespace Draco.UnitTests.IO.Extensions;

public class ArrayExtensionsTests
{
    [Theory]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, -1, new int[] { })]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, 0, new int[] { 1, 2, 3, 4, 5 })]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, 2, new int[] { 3, 4, 5 })]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, 5, new int[] { })]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, 10, new int[] { })]
    public void GetSubArray_GivenArrayAndOffset_ReturnsExpectedSubArray(int[] array, int offset, int[] expectedArray)
    {
        // Arrange

        // Act
        var result = ArrayExtensions.GetSubArray(array, offset);

        // Assert
        result.Should().BeEquivalentTo(expectedArray);
    }
}
