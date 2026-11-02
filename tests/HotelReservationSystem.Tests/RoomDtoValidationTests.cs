using System.ComponentModel.DataAnnotations;
using HotelReservationSystem.Application.DTOs;
using Xunit;

namespace HotelReservationSystem.Tests;

public class RoomDtoValidationTests
{
    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);

        Validator.TryValidateObject(model, context, results, true);

        return results;
    }

    [Fact]
    public void CreateRoomDto_WhenDataIsValid_PassesValidation()
    {
        var dto = new CreateRoomDto
        {
            Name = "Deluxe Room",
            PricePerNight = 1500,
            Capacity = 2,
            IsAvailable = true
        };

        Assert.Empty(ValidateModel(dto));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void CreateRoomDto_WhenPriceIsInvalid_FailsValidation(decimal price)
    {
        var dto = new CreateRoomDto
        {
            Name = "Deluxe Room",
            PricePerNight = price,
            Capacity = 2
        };

        Assert.NotEmpty(ValidateModel(dto));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void CreateRoomDto_WhenCapacityIsInvalid_FailsValidation(int capacity)
    {
        var dto = new CreateRoomDto
        {
            Name = "Deluxe Room",
            PricePerNight = 1500,
            Capacity = capacity
        };

        Assert.NotEmpty(ValidateModel(dto));
    }

    [Fact]
    public void CreateRoomDto_WhenNameExceeds100Characters_FailsValidation()
    {
        var dto = new CreateRoomDto
        {
            Name = new string('A', 101),
            PricePerNight = 1500,
            Capacity = 2
        };

        Assert.NotEmpty(ValidateModel(dto));
    }

    [Fact]
    public void UpdateRoomDto_WhenDataIsValid_PassesValidation()
    {
        var dto = new UpdateRoomDto
        {
            Name = "Royal Suite",
            PricePerNight = 2500,
            Capacity = 3,
            IsAvailable = true
        };

        Assert.Empty(ValidateModel(dto));
    }
}