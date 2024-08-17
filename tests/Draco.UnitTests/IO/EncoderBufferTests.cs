using Draco.IO;

namespace Draco.UnitTests.IO;

public class EncoderBufferTests
{
    [Theory]
    [InlineData(0b00110_0010, 9)]
    public void EncodeLeastSignificantBits32_GivenValueAndBitsCount_EncodesExpectedValue(uint value, byte bitsCount)
    {
        // Arrange

        // Act
        using var stream = new MemoryStream();
        using var encoderBuffer = new EncoderBuffer(new BinaryWriter(stream));
        encoderBuffer.StartBitEncoding();
        encoderBuffer.EncodeLeastSignificantBits32(bitsCount, value);
        encoderBuffer.EndBitEncoding();
        stream.Seek(0, SeekOrigin.Begin);
        using var decoderBuffer = new DecoderBuffer(new BinaryReader(stream));
        decoderBuffer.StartBitDecoding();
        var decodedValue = decoderBuffer.DecodeLeastSignificantBits32(bitsCount);
        decoderBuffer.EndBitDecoding();

        // Assert
        decodedValue.Should().Be(value);
    }

    [Theory]
    [InlineData(98)]
    [InlineData(1739)]
    public void EncodeVarIntUnsigned_GivenValue_EncodesExpectedValue(uint value)
    {
        // Arrange

        // Act
        using var stream = new MemoryStream();
        using var encoderBuffer = new EncoderBuffer(new BinaryWriter(stream));
        encoderBuffer.EncodeVarIntUnsigned(value);
        stream.Seek(0, SeekOrigin.Begin);
        using var decoderBuffer = new DecoderBuffer(new BinaryReader(stream));
        var decodedValue = decoderBuffer.DecodeVarIntUnsigned();

        // Assert
        decodedValue.Should().Be(value);
    }
}
