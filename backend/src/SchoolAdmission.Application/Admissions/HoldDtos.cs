using SchoolAdmission.Core.Entities;
using SchoolAdmission.Core.Enums;

namespace SchoolAdmission.Application.Admissions;

/// <summary>Response for POST /classes/{classId}/holds.</summary>
public sealed record HoldCreatedResponse(
    string HoldId,
    string ClassId,
    string ClassName,
    string StudentId,
    string Status,
    DateTime ExpiresAt,
    DateTime CreatedAt);

/// <summary>Response for GET /holds/{holdId} and items of GET /me/holds.</summary>
public sealed record HoldResponse(
    string HoldId,
    string ClassId,
    string ClassName,
    string Status,
    DateTime ExpiresAt,
    DateTime CreatedAt,
    DateTime? ConfirmedAt,
    DateTime? CancelledAt);

/// <summary>Response for POST /holds/{holdId}/confirm.</summary>
public sealed record ConfirmHoldResponse(
    string HoldId,
    string ClassId,
    string Status,
    DateTime? ConfirmedAt);

internal static class HoldMapping
{
    public static string ToApiValue(this HoldStatus status) => status.ToString().ToUpperInvariant();

    public static HoldResponse ToResponse(this AdmissionHold hold, DateTime nowUtc) =>
        new(
            hold.Id.ToString(),
            hold.ClassId.ToString(),
            hold.Class.Name,
            hold.EffectiveStatus(nowUtc).ToApiValue(),
            hold.ExpiresAt,
            hold.CreatedAt,
            hold.ConfirmedAt,
            hold.CancelledAt);
}
