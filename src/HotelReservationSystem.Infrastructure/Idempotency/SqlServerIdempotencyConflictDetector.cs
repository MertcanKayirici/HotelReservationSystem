using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HotelReservationSystem.Infrastructure.Persistence;

public static class SqlServerIdempotencyConflictDetector
{
    public static bool IsIdempotencyKeyConflict(DbUpdateException exception)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
        {
            if (current is SqlException sqlException &&
                (sqlException.Number == 2601 || sqlException.Number == 2627) &&
                sqlException.Message.Contains(
                    "IX_Idempotencies_Key",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
