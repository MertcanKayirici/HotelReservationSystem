using HotelReservationSystem.Application.Enums;
using HotelReservationSystem.Domain.Entities;
using HotelReservationSystem.Infrastructure.Data;
using HotelReservationSystem.Infrastructure.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelReservationSystem.Tests;

public sealed class RoomDeletionSqlServerIntegrationTests
{
    private const string ConnectionEnvironmentVariable =
        "HOTEL_RESERVATION_TEST_CONNECTION";

    public sealed class SqlServerFactAttribute : FactAttribute
    {
        public SqlServerFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable(
                    ConnectionEnvironmentVariable)))
            {
                Skip = "SQL Server integration connection is not configured.";
            }
        }
    }

    private static string GetTestConnectionString()
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                ConnectionEnvironmentVariable)!;

        var builder = new SqlConnectionStringBuilder(connectionString);

        if (builder.InitialCatalog != "HotelReservationTestDb")
        {
            throw new InvalidOperationException(
                "Integration tests must target HotelReservationTestDb.");
        }

        return connectionString;
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(GetTestConnectionString())
            .Options;

        return new AppDbContext(options);
    }

    private static Room CreateRoom() =>
        new()
        {
            Name = $"Integration Test Room {Guid.NewGuid():N}",
            PricePerNight = 1500m,
            Capacity = 2,
            IsAvailable = true,
            IsActive = true
        };

    [SqlServerFact]
    public async Task Connection_ShouldTargetDedicatedTestDatabase()
    {
        await using var context = CreateContext();

        var databaseName = await context.Database
            .SqlQueryRaw<string>("SELECT DB_NAME() AS [Value]")
            .SingleAsync();

        Assert.Equal("HotelReservationTestDb", databaseName);
    }

    [SqlServerFact]
    public async Task DeleteAsync_ShouldDeleteRoom_WhenRoomHasNoReservations()
    {
        int roomId;

        await using (var context = CreateContext())
        {
            var room = CreateRoom();

            context.Rooms.Add(room);
            await context.SaveChangesAsync();

            roomId = room.Id;
        }

        await using (var context = CreateContext())
        {
            var repository = new RoomRepository(context);

            var result = await repository.DeleteAsync(roomId);

            Assert.Equal(RoomDeletionResult.Deleted, result);
        }

        await using (var context = CreateContext())
        {
            var room = await context.Rooms
                .AsNoTracking()
                .SingleOrDefaultAsync(room => room.Id == roomId);

            Assert.Null(room);
        }
    }

    [SqlServerFact]
    public async Task DeleteAsync_ShouldReturnHasDependencies_WhenRoomHasReservation()
    {
        int roomId;
        int reservationId;

        await using (var context = CreateContext())
        {
            var room = CreateRoom();

            context.Rooms.Add(room);
            await context.SaveChangesAsync();

            roomId = room.Id;

            var reservation = new Reservation
            {
                RoomId = roomId,
                UserId = 0,
                CheckInDate = DateTime.UtcNow.Date.AddDays(1),
                CheckOutDate = DateTime.UtcNow.Date.AddDays(2),
                CreatedAt = DateTime.UtcNow
            };

            context.Reservations.Add(reservation);
            await context.SaveChangesAsync();

            reservationId = reservation.Id;
        }

        try
        {
            await using var context = CreateContext();

            var repository = new RoomRepository(context);

            var result = await repository.DeleteAsync(roomId);

            Assert.Equal(RoomDeletionResult.HasDependencies, result);

            var roomStillExists = await context.Rooms
                .AsNoTracking()
                .AnyAsync(room => room.Id == roomId);

            Assert.True(roomStillExists);
        }
        finally
        {
            await using var cleanupContext = CreateContext();

            var reservation = await cleanupContext.Reservations
                .SingleOrDefaultAsync(item => item.Id == reservationId);

            if (reservation is not null)
            {
                cleanupContext.Reservations.Remove(reservation);
            }

            var room = await cleanupContext.Rooms
                .SingleOrDefaultAsync(item => item.Id == roomId);

            if (room is not null)
            {
                cleanupContext.Rooms.Remove(room);
            }

            await cleanupContext.SaveChangesAsync();
        }
    }

    [SqlServerFact]
    public async Task DeleteAsync_ShouldReturnNotFound_WhenRoomDoesNotExist()
    {
        await using var context = CreateContext();

        var repository = new RoomRepository(context);

        var nonexistentId = -Guid.NewGuid().GetHashCode();

        var result = await repository.DeleteAsync(nonexistentId);

        Assert.Equal(RoomDeletionResult.NotFound, result);
    }
}
