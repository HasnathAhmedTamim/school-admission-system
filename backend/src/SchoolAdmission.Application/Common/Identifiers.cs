using SchoolAdmission.Application.Common.Exceptions;

namespace SchoolAdmission.Application.Common;

/// <summary>Parses route identifiers. Malformed IDs are reported as "not found", never as server errors.</summary>
public static class Identifiers
{
    public static Guid ParseClassId(string? value) =>
        Guid.TryParse(value, out var id) ? id : throw ClassNotFound();

    public static Guid ParseHoldId(string? value) =>
        Guid.TryParse(value, out var id) ? id : throw HoldNotFound();

    public static NotFoundException ClassNotFound() =>
        new("CLASS_NOT_FOUND", "This class does not exist. It may have been removed or the link is wrong.");

    public static NotFoundException HoldNotFound() =>
        new("HOLD_NOT_FOUND", "This application does not exist. Please check the application ID.");
}
