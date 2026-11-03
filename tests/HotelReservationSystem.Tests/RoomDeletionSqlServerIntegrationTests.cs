
using HotelReservationSystem.Infrastructure.Data;
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

    [SqlServerFact]
    public async Task Connection_ShouldTargetDedicatedTestDatabase()
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                ConnectionEnvironmentVariable)!;

        var builder = new SqlConnectionStringBuilder(connectionString);

        Assert.Equal("HotelReservationTestDb", builder.InitialCatalog);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var context = new AppDbContext(options);

        var databaseName = await context.Database
            .SqlQueryRaw<string>("SELECT DB_NAME() AS [Value]")
            .SingleAsync();

        Assert.Equal("HotelReservationTestDb", databaseName);
    }
}
