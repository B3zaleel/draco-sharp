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

    [Theory]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, -1, 1, new int[] { })]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, 0, -1, new int[] { })]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, 0, 6, new int[] { })]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, 5, 1, new int[] { })]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, 10, 1, new int[] { })]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, 0, 5, new int[] { 1, 2, 3, 4, 5 })]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, 1, 3, new int[] { 2, 3, 4 })]
    public void GetSubArray_GivenArrayAndOffsetAndCount_ReturnsExpectedSubArray(int[] array, int offset, int count, int[] expectedArray)
    {
        // Arrange

        // Act
        var result = ArrayExtensions.GetSubArray(array, offset, count);

        // Assert
        result.Should().BeEquivalentTo(expectedArray);
    }

    [Theory]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, new int[] { 7 }, -1, new int[] { 1, 2, 3, 4, 5 })]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, new int[] { 7 }, 5, new int[] { 1, 2, 3, 4, 5 })]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, new int[] { 7 }, 10, new int[] { 1, 2, 3, 4, 5 })]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, new int[] { 7, 8, 9, 10 }, 3, new int[] { 1, 2, 3, 4, 5 })]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, new int[] { 7 }, 4, new int[] { 1, 2, 3, 4, 7 })]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, new int[] { 7 }, 1, new int[] { 1, 7, 3, 4, 5 })]
    public void SetSubArray_GivenArrayAndSubArrayAndOffset_OverwritesExpectedArraySection(int[] array, int[] subArray, int offset, int[] expectedArray)
    {
        // Arrange

        // Act
        ArrayExtensions.SetSubArray(array, subArray, offset);

        // Assert
        array.Should().BeEquivalentTo(expectedArray);
    }
}
