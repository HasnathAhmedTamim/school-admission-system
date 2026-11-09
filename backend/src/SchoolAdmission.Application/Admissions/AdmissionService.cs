using SchoolAdmission.Application.Common;
using SchoolAdmission.Application.Common.Exceptions;
using SchoolAdmission.Core.Entities;
using SchoolAdmission.Core.Enums;

namespace SchoolAdmission.Application.Admissions;

public interface IAdmissionService
{
    Task<HoldCreatedResponse> ApplyAsync(string classId, string? studentIdHeader, CancellationToken cancellationToken);

    Task<HoldResponse> GetHoldAsync(string holdId, string? studentIdHeader, CancellationToken cancellationToken);

    Task<ConfirmHoldResponse> ConfirmAsync(string holdId, string? studentIdHeader, CancellationToken cancellationToken);

    Task CancelAsync(string holdId, string? studentIdHeader, CancellationToken cancellationToken);

    Task<IReadOnlyList<HoldResponse>> GetMyHoldsAsync(string? studentIdHeader, CancellationToken cancellationToken);
}

public sealed class AdmissionService(
    IAdmissionHoldRepository holds,
    HoldSettings settings,
    TimeProvider clock) : IAdmissionService
{
    public async Task<HoldCreatedResponse> ApplyAsync(string classId, string? studentIdHeader, CancellationToken cancellationToken)
    {
        var studentId = StudentIdentity.Require(studentIdHeader);
        var id = Identifiers.ParseClassId(classId);

        // Read before waiting for the class lock. An earlier time can only count more holds as still active,
        // never fewer, so it cannot cause overbooking.
        var result = await holds.CreateHoldAsync(id, studentId, UtcNow(), settings.Duration, cancellationToken);

        return result.Outcome switch
        {
            CreateHoldOutcome.Created => ToCreatedResponse(result.Hold!),
            CreateHoldOutcome.ClassNotFound => throw Identifiers.ClassNotFound(),
            CreateHoldOutcome.NoSeatsAvailable => throw new ConflictException(
                "NO_SEATS_AVAILABLE",
                "Sorry, all seats in this class are taken right now. A seat may open up if another application expires or is cancelled."),
            CreateHoldOutcome.AlreadyHeld => throw new ConflictException(
                "ALREADY_APPLIED",
                "This student already has an active application for this class. Please pay the fee or cancel it first."),
            CreateHoldOutcome.AlreadyConfirmed => throw new ConflictException(
                "ALREADY_ADMITTED",
                "This student's admission to this class is already confirmed."),
            _ => throw new InvalidOperationException($"Unexpected outcome {result.Outcome}.")
        };
    }

    public async Task<HoldResponse> GetHoldAsync(string holdId, string? studentIdHeader, CancellationToken cancellationToken)
    {
        var studentId = StudentIdentity.Optional(studentIdHeader);
        var id = Identifiers.ParseHoldId(holdId);

        var hold = await holds.GetAsync(id, cancellationToken) ?? throw Identifiers.HoldNotFound();
        if (studentId is not null && !hold.BelongsTo(studentId))
        {
            throw NotYourApplication();
        }

        return hold.ToResponse(UtcNow());
    }

    public async Task<ConfirmHoldResponse> ConfirmAsync(string holdId, string? studentIdHeader, CancellationToken cancellationToken)
    {
        var studentId = StudentIdentity.Require(studentIdHeader);
        var id = Identifiers.ParseHoldId(holdId);

        // The clock is read after the locks are acquired: a request delayed while waiting for them
        // must not confirm a hold that expired (and whose seat was given away) in the meantime.
        var locked = await holds.ModifyAsync(id, hold =>
        {
            EnsureOwner(hold, studentId);
            return hold.Confirm(UtcNow());
        }, cancellationToken) ?? throw Identifiers.HoldNotFound();

        return locked.Result switch
        {
            HoldTransitionResult.Success or HoldTransitionResult.AlreadyConfirmed => new ConfirmHoldResponse(
                locked.Hold.Id.ToString(),
                locked.Hold.ClassId.ToString(),
                HoldStatus.Confirmed.ToApiValue(),
                locked.Hold.ConfirmedAt),
            HoldTransitionResult.Expired => throw HoldExpired(),
            HoldTransitionResult.AlreadyCancelled => throw new ConflictException(
                "HOLD_CANCELLED",
                "This application was cancelled, so the fee cannot be paid. Please apply again if seats are available."),
            _ => throw new InvalidOperationException($"Unexpected transition result {locked.Result}.")
        };
    }

    public async Task CancelAsync(string holdId, string? studentIdHeader, CancellationToken cancellationToken)
    {
        var studentId = StudentIdentity.Require(studentIdHeader);
        var id = Identifiers.ParseHoldId(holdId);

        var locked = await holds.ModifyAsync(id, hold =>
        {
            EnsureOwner(hold, studentId);
            return hold.Cancel(UtcNow());
        }, cancellationToken) ?? throw Identifiers.HoldNotFound();

        switch (locked.Result)
        {
            case HoldTransitionResult.Success:
            case HoldTransitionResult.AlreadyCancelled:
                return;
            case HoldTransitionResult.AlreadyConfirmed:
                throw new ConflictException(
                    "ADMISSION_ALREADY_CONFIRMED",
                    "The admission fee has already been paid, so this application can no longer be cancelled.");
            case HoldTransitionResult.Expired:
                throw HoldExpired();
            default:
                throw new InvalidOperationException($"Unexpected transition result {locked.Result}.");
        }
    }

    public async Task<IReadOnlyList<HoldResponse>> GetMyHoldsAsync(string? studentIdHeader, CancellationToken cancellationToken)
    {
        var studentId = StudentIdentity.Require(studentIdHeader);
        var now = UtcNow();

        var studentHolds = await holds.GetByStudentAsync(studentId, cancellationToken);
        return studentHolds.Select(h => h.ToResponse(now)).ToList();
    }

    // Millisecond precision keeps timestamps identical between the create response and later reads
    // (PostgreSQL stores microseconds, .NET ticks are 100ns).
    private DateTime UtcNow()
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMillisecond));
    }

    private static void EnsureOwner(AdmissionHold hold, string studentId)
    {
        if (!hold.BelongsTo(studentId))
        {
            throw NotYourApplication();
        }
    }

    private static HoldCreatedResponse ToCreatedResponse(AdmissionHold hold) =>
        new(
            hold.Id.ToString(),
            hold.ClassId.ToString(),
            hold.Class.Name,
            hold.StudentId,
            hold.Status.ToApiValue(),
            hold.ExpiresAt,
            hold.CreatedAt);

    private static ForbiddenException NotYourApplication() =>
        new("NOT_YOUR_APPLICATION", "This application belongs to a different student.");

    private static ConflictException HoldExpired() =>
        new("HOLD_EXPIRED", "The time to pay the admission fee has run out and the seat was released. Please apply again if seats are available.");
}
