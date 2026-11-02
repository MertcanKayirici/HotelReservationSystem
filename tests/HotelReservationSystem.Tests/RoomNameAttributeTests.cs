using HotelReservationSystem.Application.Validation;
using Xunit;

namespace HotelReservationSystem.Tests;

public class RoomNameAttributeTests
{
    private readonly RoomNameAttribute _attribute = new();

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void IsValid_WhenNameIsEmptyOrWhitespace_ReturnsFalse(string name)
    {
        var result = _attribute.IsValid(name);

        Assert.False(result);
    }

    [Theory]
    [InlineData("Deluxe Room")]
    [InlineData("Royal Suite")]
    public void IsValid_WhenNameIsValid_ReturnsTrue(string name)
    {
        var result = _attribute.IsValid(name);

        Assert.True(result);
    }

    [Fact]
    public void IsValid_WhenNameIsNull_ReturnsFalse()
    {
        var result = _attribute.IsValid(null);

        Assert.False(result);
    }
}