using Draco.IO;

namespace Draco.UnitTests.IO;

public class ConstantsTests
{
    [Theory]
    [InlineData(typeof(int), typeof(uint), new int[] { 3 }, new object[] { 3 })]
    [InlineData(typeof(int), typeof(uint), new int[] { -3 }, new object[] { 4294967293 })]
    [InlineData(typeof(int), typeof(float), new int[] { -3 }, new object[] { -float.NaN })]
    public void ReinterpretCast_GivenIntValues_ShouldReturnReinterpretedValues(Type srcType, Type destinationType, int[] srcValues, object[] expectedValues)
    {
        // Arrange
        var method = typeof(Constants).GetMethod(nameof(Constants.ReinterpretCast))!.MakeGenericMethod(srcType, destinationType);

        // Act
        var result = method.Invoke(null, [srcValues]);

        // Assert
        result.Should().BeEquivalentTo(expectedValues);
    }
}
