using SchoolAdmission.Core.Entities;

namespace SchoolAdmission.Application.Admissions;

public enum CreateHoldOutcome
{
    Created,
    ClassNotFound,
    NoSeatsAvailable,
    AlreadyHeld,
    AlreadyConfirmed
}

public sealed record CreateHoldResult(CreateHoldOutcome Outcome, AdmissionHold? Hold = null);

public sealed record LockedHoldResult<T>(AdmissionHold Hold, T Result);

public interface IAdmissionHoldRepository
{
    /// <summary>
    /// Atomically reserves a seat: locks the class row, checks the student's existing holds and the
    /// remaining capacity, and inserts the hold in one transaction.
    /// </summary>
    Task<CreateHoldResult> CreateHoldAsync(
        Guid classId, string studentId, DateTime nowUtc, TimeSpan duration, CancellationToken cancellationToken);

    /// <summary>
    /// Locks the hold's class row (shared) and then the hold row, applies <paramref name="modify"/> and saves
    /// the result in one transaction. Returns null if the hold does not exist. Exceptions thrown by
    /// <paramref name="modify"/> roll back. <paramref name="modify"/> runs while the locks are held, so
    /// time-dependent checks must read the clock inside it, not before calling this method.
    /// </summary>
    Task<LockedHoldResult<T>?> ModifyAsync<T>(
        Guid holdId, Func<AdmissionHold, T> modify, CancellationToken cancellationToken);

    Task<AdmissionHold?> GetAsync(Guid holdId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AdmissionHold>> GetByStudentAsync(string studentId, CancellationToken cancellationToken);
}
