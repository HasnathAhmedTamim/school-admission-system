namespace SchoolAdmission.Application.Classes;

/// <summary>Request body for POST /classes. Fields are nullable so missing values produce validation errors.</summary>
public sealed record CreateClassRequest(string? Name, int? TotalSeats);

/// <summary>A class with its live seat statistics (GET /classes, GET /classes/{classId}, POST /classes).</summary>
public sealed record ClassResponse(
    string Id,
    string Name,
    int TotalSeats,
    int HeldSeats,
    int ConfirmedSeats,
    int AvailableSeats,
    DateTime CreatedAt);

/// <summary>Seat counts computed from authoritative hold rows at a given instant.</summary>
public sealed record ClassSeatSummary(
    Guid Id,
    string Name,
    int TotalSeats,
    int HeldSeats,
    int ConfirmedSeats,
    DateTime CreatedAt)
{
    public int AvailableSeats => Math.Max(0, TotalSeats - HeldSeats - ConfirmedSeats);

    public ClassResponse ToResponse() =>
        new(Id.ToString(), Name, TotalSeats, HeldSeats, ConfirmedSeats, AvailableSeats, CreatedAt);
}
