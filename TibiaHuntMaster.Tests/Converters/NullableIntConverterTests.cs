using System.Globalization;

using Avalonia.Data;

using FluentAssertions;

using TibiaHuntMaster.App.Converters;

namespace TibiaHuntMaster.Tests.Converters
{
    public sealed class NullableIntConverterTests
    {
        [Fact]
        public void Convert_ShouldRenderLongValue_ForTextBinding()
        {
            // Arrange
            NullableIntConverter sut = new();

            // Act
            object? result = sut.Convert(700L, typeof(string), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().Be("700");
        }

        [Fact]
        public void ConvertBack_ShouldParseLong_WhenTargetTypeIsLong()
        {
            // Arrange
            NullableIntConverter sut = new();

            // Act
            object? result = sut.ConvertBack("700", typeof(long), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().BeOfType<long>().Which.Should().Be(700L);
        }

        [Fact]
        public void ConvertBack_ShouldReturnBindingError_WhenIntTargetIsOutOfRange()
        {
            // Arrange
            NullableIntConverter sut = new();

            // Act
            object? result = sut.ConvertBack("9999999999", typeof(int), null, CultureInfo.InvariantCulture);

            // Assert
            result.Should().BeOfType<BindingNotification>();
        }
    }
}
