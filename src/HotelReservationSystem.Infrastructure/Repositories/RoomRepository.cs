
using System.Data;
using HotelReservationSystem.Application.Enums;
using HotelReservationSystem.Application.Interfaces.Repositories;
using HotelReservationSystem.Domain.Entities;
using HotelReservationSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HotelReservationSystem.Infrastructure.Repositories
{
    public class RoomRepository : IRoomRepository
    {
        private readonly AppDbContext _context;

        public RoomRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Room>> GetAllAsync()
        {
            return await _context.Rooms
                .AsNoTracking()
                .OrderBy(room => room.Id)
                .ToListAsync();
        }

        public async Task<Room?> GetByIdAsync(int id)
        {
            return await _context.Rooms.FindAsync(id);
        }

        public async Task AddAsync(Room room)
        {
            await _context.Rooms.AddAsync(room);
        }

        public async Task<RoomDeletionResult> DeleteAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

            try
            {
                var connection = _context.Database.GetDbConnection();

                await using var command = connection.CreateCommand();

                command.Transaction = _context.Database
                    .CurrentTransaction!
                    .GetDbTransaction();

                command.CommandText = """
                    DECLARE @result int;

                    EXEC @result = sys.sp_getapplock
                        @Resource = @resource,
                        @LockMode = 'Exclusive',
                        @LockOwner = 'Transaction',
                        @LockTimeout = 10000;

                    IF @result < 0
                    BEGIN
                        THROW 50001, 'Could not acquire room lock.', 1;
                    END;

                    DECLARE @hasDependencies bit = 0;
                    DECLARE @schemaName sysname;
                    DECLARE @tableName sysname;
                    DECLARE @columnName sysname;
                    DECLARE @sql nvarchar(max);

                    DECLARE @roomsObjectId int = OBJECT_ID(N'dbo.Rooms', N'U');

                    IF @roomsObjectId IS NULL
                    BEGIN
                        THROW 50002, 'dbo.Rooms table was not found.', 1;
                    END;

                    DECLARE @roomIdColumnId int =
                        CONVERT(int, COLUMNPROPERTY(@roomsObjectId, 'Id', 'ColumnId'));

                    IF @roomIdColumnId IS NULL
                    BEGIN
                        THROW 50003, 'Rooms.Id column was not found.', 1;
                    END;

                    DECLARE dependency_cursor CURSOR LOCAL FAST_FORWARD
                    FOR
                        SELECT
                            s.name,
                            t.name,
                            c.name
                        FROM sys.foreign_key_columns fkc
                        INNER JOIN sys.tables t
                            ON t.object_id = fkc.parent_object_id
                        INNER JOIN sys.schemas s
                            ON s.schema_id = t.schema_id
                        INNER JOIN sys.columns c
                            ON c.object_id = fkc.parent_object_id
                            AND c.column_id = fkc.parent_column_id
                        WHERE fkc.referenced_object_id = @roomsObjectId
                    AND fkc.referenced_column_id = @roomIdColumnId;

                    OPEN dependency_cursor;

                    FETCH NEXT FROM dependency_cursor
                    INTO @schemaName, @tableName, @columnName;

                    WHILE @@FETCH_STATUS = 0
                    BEGIN
                        SET @sql =
                            N'IF EXISTS (SELECT 1 FROM '
                            + QUOTENAME(@schemaName)
                            + N'.'
                            + QUOTENAME(@tableName)
                            + N' WHERE '
                            + QUOTENAME(@columnName)
                            + N' = @roomId)
                            SET @hasDependencies = 1;';

                        EXEC sys.sp_executesql
                            @sql,
                            N'@roomId int, @hasDependencies bit OUTPUT',
                            @roomId = @roomId,
                            @hasDependencies = @hasDependencies OUTPUT;

                        IF @hasDependencies = 1
                            BREAK;

                        FETCH NEXT FROM dependency_cursor
                        INTO @schemaName, @tableName, @columnName;
                    END;

                    CLOSE dependency_cursor;
                    DEALLOCATE dependency_cursor;

                    SELECT @hasDependencies;
                    """;

                var resourceParameter = command.CreateParameter();
                resourceParameter.ParameterName = "@resource";
                resourceParameter.Value = $"HotelReservation:Room:{id}";
                command.Parameters.Add(resourceParameter);

                var roomIdParameter = command.CreateParameter();
                roomIdParameter.ParameterName = "@roomId";
                roomIdParameter.Value = id;
                command.Parameters.Add(roomIdParameter);

                var dependencyResult =
                    await command.ExecuteScalarAsync(cancellationToken);

                var hasDependencies =
                    Convert.ToBoolean(dependencyResult);

                var room = await _context.Rooms
                    .FirstOrDefaultAsync(
                        x => x.Id == id,
                        cancellationToken);

                if (room is null)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return RoomDeletionResult.NotFound;
                }

                if (hasDependencies)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return RoomDeletionResult.HasDependencies;
                }

                _context.Rooms.Remove(room);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return RoomDeletionResult.Deleted;
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<Room?> SetActiveWithLockAsync(
            int id,
            bool isActive,
            CancellationToken cancellationToken = default)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var connection = _context.Database.GetDbConnection();

                await using var command = connection.CreateCommand();

                command.Transaction = _context.Database
                    .CurrentTransaction!
                    .GetDbTransaction();

                command.CommandText = """
                    DECLARE @result int;

                    EXEC @result = sys.sp_getapplock
                        @Resource = @resource,
                        @LockMode = 'Exclusive',
                        @LockOwner = 'Transaction',
                        @LockTimeout = 10000;

                    SELECT @result;
                    """;

                var resourceParameter = command.CreateParameter();
                resourceParameter.ParameterName = "@resource";
                resourceParameter.Value = $"HotelReservation:Room:{id}";

                command.Parameters.Add(resourceParameter);

                var lockResultObject =
                    await command.ExecuteScalarAsync(cancellationToken);

                var lockResult = Convert.ToInt32(lockResultObject);

                if (lockResult < 0)
                {
                    throw new InvalidOperationException(
                        $"Could not acquire room lock. SQL result: {lockResult}");
                }

                var room = await _context.Rooms
                    .FirstOrDefaultAsync(
                        x => x.Id == id,
                        cancellationToken);

                if (room is null)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return null;
                }

                room.IsActive = isActive;

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return room;
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        }
    }
}
