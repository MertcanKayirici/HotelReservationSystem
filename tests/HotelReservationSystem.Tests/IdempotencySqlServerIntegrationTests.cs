using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using HotelReservationSystem.Infrastructure.Data;
using Xunit;

namespace HotelReservationSystem.Tests;

public sealed class IdempotencySqlServerIntegrationTests
{
    private const string ConnectionEnvironmentVariable =
        "HOTEL_RESERVATION_TEST_CONNECTION";

    [SqlServerFact]
    public async Task DuplicateIdempotencyKey_ShouldBeRejectedBySqlServer()
    {
        var connectionString =
            Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable);

        var databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;

        Assert.Equal("HotelReservationTestDb", databaseName);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        var key = $"integration-test-{Guid.NewGuid():N}";

        try
        {
            await using (var firstContext = new AppDbContext(options))
            {
                firstContext.Idempotencies.Add(new()
                {
                    Key = key,
                    Status = "PENDING",
                    RequestHash = "hash-1",
                    Response = string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    UserId = null
                });

                await firstContext.SaveChangesAsync();
            }

            await using var secondContext = new AppDbContext(options);

            secondContext.Idempotencies.Add(new()
            {
                Key = key,
                Status = "PENDING",
                RequestHash = "hash-2",
                Response = string.Empty,
                CreatedAt = DateTime.UtcNow,
                UserId = null
            });

            var exception = await Assert.ThrowsAsync<DbUpdateException>(
                () => secondContext.SaveChangesAsync());

            var sqlException = Assert.IsType<SqlException>(exception.InnerException);

            Assert.Contains(sqlException.Number, new[] { 2601, 2627 });
        }
        finally
        {
            await using var cleanupContext = new AppDbContext(options);

            var existingRecords = await cleanupContext.Idempotencies
                .Where(x => x.Key == key)
                .ToListAsync();

            cleanupContext.Idempotencies.RemoveRange(existingRecords);
            await cleanupContext.SaveChangesAsync();
        }
    }

    public sealed class SqlServerFactAttribute : FactAttribute
    {
        public SqlServerFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable(
                    "HOTEL_RESERVATION_TEST_CONNECTION")))
            {
                Skip = "SQL Server integration connection is not configured.";
            }
        }
    }

    [SqlServerFact]
    public async Task ConcurrentDuplicateKeys_ShouldPersistOnlyOneRecord()
    {
        var connectionString =
            Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable)!;

        var databaseName =
            new SqlConnectionStringBuilder(connectionString).InitialCatalog;

        Assert.Equal("HotelReservationTestDb", databaseName);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        var key = $"concurrent-test-{Guid.NewGuid():N}";

        async Task<bool> TryInsertAsync(string requestHash)
        {
            await using var context = new AppDbContext(options);

            context.Idempotencies.Add(new()
            {
                Key = key,
                Status = "PENDING",
                RequestHash = requestHash,
                Response = string.Empty,
                CreatedAt = DateTime.UtcNow,
                UserId = null
            });

            try
            {
                await context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is SqlException sqlException
                    && (sqlException.Number == 2601
                        || sqlException.Number == 2627))
            {
                return false;
            }
        }

        try
        {
            var results = await Task.WhenAll(
                TryInsertAsync("hash-1"),
                TryInsertAsync("hash-2"));

            Assert.Single(results, inserted => inserted);

            await using var verificationContext = new AppDbContext(options);

            var recordCount = await verificationContext.Idempotencies
                .CountAsync(x => x.Key == key);

            Assert.Equal(1, recordCount);
        }
        finally
        {
            await using var cleanupContext = new AppDbContext(options);

            var records = await cleanupContext.Idempotencies
                .Where(x => x.Key == key)
                .ToListAsync();

            cleanupContext.Idempotencies.RemoveRange(records);
            await cleanupContext.SaveChangesAsync();
        }
    }

}
