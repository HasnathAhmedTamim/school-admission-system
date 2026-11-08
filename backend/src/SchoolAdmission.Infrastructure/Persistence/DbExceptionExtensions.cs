using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace SchoolAdmission.Infrastructure.Persistence;

internal static class DbExceptionExtensions
{
    public static bool IsUniqueViolation(this DbUpdateException exception, string constraintName) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        } pg && pg.ConstraintName == constraintName;
}
