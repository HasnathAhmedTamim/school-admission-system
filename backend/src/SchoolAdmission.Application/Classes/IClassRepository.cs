using SchoolAdmission.Core.Entities;

namespace SchoolAdmission.Application.Classes;

public interface IClassRepository
{
    Task<bool> NameExistsAsync(string normalizedName, CancellationToken cancellationToken);

    /// <summary>Persists a new class. Throws a ConflictException if the name is already taken.</summary>
    Task AddAsync(SchoolClass schoolClass, CancellationToken cancellationToken);

    Task<IReadOnlyList<ClassSeatSummary>> GetSummariesAsync(DateTime nowUtc, CancellationToken cancellationToken);

    Task<ClassSeatSummary?> GetSummaryAsync(Guid classId, DateTime nowUtc, CancellationToken cancellationToken);
}
