using SchoolAdmission.Core.Enums;

namespace SchoolAdmission.Core.Entities;

/// <summary>
/// A time-limited seat reservation for one student in one class.
/// A HELD hold past its ExpiresAt is treated as EXPIRED everywhere, even before
/// the background worker persists that status.
/// </summary>
public class AdmissionHold
{
    public const int StudentIdLength = 17;

    private AdmissionHold()
    {
    }

    public static AdmissionHold Create(Guid classId, string studentId, DateTime nowUtc, TimeSpan duration)
    {
        return new AdmissionHold
        {
            Id = Guid.CreateVersion7(),
            ClassId = classId,
            StudentId = studentId,
            Status = HoldStatus.Held,
            CreatedAt = nowUtc,
            ExpiresAt = nowUtc.Add(duration)
        };
    }

    public Guid Id { get; private set; }

    public Guid ClassId { get; private set; }

    public string StudentId { get; private set; } = null!;

    public HoldStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime? ConfirmedAt { get; private set; }

    public DateTime? CancelledAt { get; private set; }

    public SchoolClass Class { get; private set; } = null!;

    public bool IsActive(DateTime nowUtc) => Status == HoldStatus.Held && ExpiresAt > nowUtc;

    public bool IsExpired(DateTime nowUtc) => Status == HoldStatus.Held && ExpiresAt <= nowUtc;

    public HoldStatus EffectiveStatus(DateTime nowUtc) => IsExpired(nowUtc) ? HoldStatus.Expired : Status;

    public bool BelongsTo(string studentId) => string.Equals(StudentId, studentId, StringComparison.Ordinal);

    public HoldTransitionResult Confirm(DateTime nowUtc)
    {
        if (Status == HoldStatus.Confirmed)
        {
            return HoldTransitionResult.AlreadyConfirmed;
        }

        if (MarkExpiredIfDue(nowUtc) || Status == HoldStatus.Expired)
        {
            return HoldTransitionResult.Expired;
        }

        if (Status == HoldStatus.Cancelled)
        {
            return HoldTransitionResult.AlreadyCancelled;
        }

        Status = HoldStatus.Confirmed;
        ConfirmedAt = nowUtc;
        return HoldTransitionResult.Success;
    }

    public HoldTransitionResult Cancel(DateTime nowUtc)
    {
        if (Status == HoldStatus.Cancelled)
        {
            return HoldTransitionResult.AlreadyCancelled;
        }

        if (Status == HoldStatus.Confirmed)
        {
            return HoldTransitionResult.AlreadyConfirmed;
        }

        if (MarkExpiredIfDue(nowUtc) || Status == HoldStatus.Expired)
        {
            return HoldTransitionResult.Expired;
        }

        Status = HoldStatus.Cancelled;
        CancelledAt = nowUtc;
        return HoldTransitionResult.Success;
    }

    /// <returns>true if the hold transitioned from HELD to EXPIRED.</returns>
    public bool MarkExpiredIfDue(DateTime nowUtc)
    {
        if (!IsExpired(nowUtc))
        {
            return false;
        }

        Status = HoldStatus.Expired;
        return true;
    }
}
