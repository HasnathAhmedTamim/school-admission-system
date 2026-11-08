using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Application.Classes;
using SchoolAdmission.Core.Entities;
using SchoolAdmission.Core.Enums;

namespace SchoolAdmission.Infrastructure.Persistence.Repositories;

public sealed class ClassRepository(AppDbContext db) : IClassRepository
{
    public Task<bool> NameExistsAsync(string normalizedName, CancellationToken cancellationToken) =>
        db.Classes.AnyAsync(c => c.NormalizedName == normalizedName, cancellationToken);

    public async Task AddAsync(SchoolClass schoolClass, CancellationToken cancellationToken)
    {
        db.Classes.Add(schoolClass);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation("ux_classes_normalized_name"))
        {
            throw ClassService.DuplicateName(schoolClass.Name);
        }
    }

    public async Task<IReadOnlyList<ClassSeatSummary>> GetSummariesAsync(DateTime nowUtc, CancellationToken cancellationToken) =>
        await SummaryQuery(db.Classes.OrderBy(c => c.Name), nowUtc)
            .ToListAsync(cancellationToken);

    public Task<ClassSeatSummary?> GetSummaryAsync(Guid classId, DateTime nowUtc, CancellationToken cancellationToken) =>
        SummaryQuery(db.Classes.Where(c => c.Id == classId), nowUtc)
            .FirstOrDefaultAsync(cancellationToken);

    // Held seats only count holds that have not passed their expiry time, so a seat is released
    // the moment a hold expires, independent of the background expiry worker.
    private static IQueryable<ClassSeatSummary> SummaryQuery(IQueryable<SchoolClass> classes, DateTime nowUtc) =>
        classes
            .AsNoTracking()
            .Select(c => new ClassSeatSummary(
                c.Id,
                c.Name,
                c.TotalSeats,
                c.Holds.Count(h => h.Status == HoldStatus.Held && h.ExpiresAt > nowUtc),
                c.Holds.Count(h => h.Status == HoldStatus.Confirmed),
                c.CreatedAt));
}
