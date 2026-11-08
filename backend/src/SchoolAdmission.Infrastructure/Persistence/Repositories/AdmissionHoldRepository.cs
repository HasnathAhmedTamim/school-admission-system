using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Application.Admissions;
using SchoolAdmission.Core.Entities;
using SchoolAdmission.Core.Enums;
using SchoolAdmission.Infrastructure.Persistence.Configurations;

namespace SchoolAdmission.Infrastructure.Persistence.Repositories;

/// <remarks>
/// Lock ordering: CreateHoldAsync locks the class row and then (at most) the student's own stale hold;
/// ModifyAsync locks only a single hold row; the expiry worker uses SKIP LOCKED. No code path waits for
/// a class lock while holding a hold lock, so these transactions cannot deadlock each other.
/// </remarks>
public sealed class AdmissionHoldRepository(AppDbContext db) : IAdmissionHoldRepository
{
    public async Task<CreateHoldResult> CreateHoldAsync(
        Guid classId, string studentId, DateTime nowUtc, TimeSpan duration, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Serializes all seat reservations for this class across every API instance.
        var schoolClass = (await db.Classes
                .FromSql($"SELECT * FROM classes WHERE id = {classId} FOR UPDATE")
                .ToListAsync(cancellationToken))
            .SingleOrDefault();

        if (schoolClass is null)
        {
            return new CreateHoldResult(CreateHoldOutcome.ClassNotFound);
        }

        // A stale HELD row would otherwise block the student via the partial unique index.
        await db.AdmissionHolds
            .Where(h => h.ClassId == classId
                        && h.StudentId == studentId
                        && h.Status == HoldStatus.Held
                        && h.ExpiresAt <= nowUtc)
            .ExecuteUpdateAsync(s => s.SetProperty(h => h.Status, HoldStatus.Expired), cancellationToken);

        var existingStatus = await db.AdmissionHolds
            .Where(h => h.ClassId == classId
                        && h.StudentId == studentId
                        && (h.Status == HoldStatus.Held || h.Status == HoldStatus.Confirmed))
            .Select(h => (HoldStatus?)h.Status)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingStatus is not null)
        {
            return new CreateHoldResult(existingStatus == HoldStatus.Confirmed
                ? CreateHoldOutcome.AlreadyConfirmed
                : CreateHoldOutcome.AlreadyHeld);
        }

        var occupiedSeats = await db.AdmissionHolds.CountAsync(
            h => h.ClassId == classId
                 && ((h.Status == HoldStatus.Held && h.ExpiresAt > nowUtc) || h.Status == HoldStatus.Confirmed),
            cancellationToken);

        if (occupiedSeats >= schoolClass.TotalSeats)
        {
            return new CreateHoldResult(CreateHoldOutcome.NoSeatsAvailable);
        }

        var hold = AdmissionHold.Create(classId, studentId, nowUtc, duration);
        db.AdmissionHolds.Add(hold);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation(AdmissionHoldConfiguration.ActiveStudentIndexName))
        {
            return new CreateHoldResult(CreateHoldOutcome.AlreadyHeld);
        }

        await transaction.CommitAsync(cancellationToken);
        return new CreateHoldResult(CreateHoldOutcome.Created, hold);
    }

    public async Task<LockedHoldResult<T>?> ModifyAsync<T>(
        Guid holdId, Func<AdmissionHold, T> modify, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var hold = (await db.AdmissionHolds
                .FromSql($"SELECT * FROM admission_holds WHERE id = {holdId} FOR UPDATE")
                .ToListAsync(cancellationToken))
            .SingleOrDefault();

        if (hold is null)
        {
            return null;
        }

        await db.Entry(hold).Reference(h => h.Class).LoadAsync(cancellationToken);

        var result = modify(hold);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new LockedHoldResult<T>(hold, result);
    }

    public Task<AdmissionHold?> GetAsync(Guid holdId, CancellationToken cancellationToken) =>
        db.AdmissionHolds
            .AsNoTracking()
            .Include(h => h.Class)
            .FirstOrDefaultAsync(h => h.Id == holdId, cancellationToken);

    public async Task<IReadOnlyList<AdmissionHold>> GetByStudentAsync(string studentId, CancellationToken cancellationToken) =>
        await db.AdmissionHolds
            .AsNoTracking()
            .Include(h => h.Class)
            .Where(h => h.StudentId == studentId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);
}
