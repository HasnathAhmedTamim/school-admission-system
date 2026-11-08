using SchoolAdmission.Application.Common.Exceptions;
using SchoolAdmission.Core.Entities;

namespace SchoolAdmission.Application.Admissions;

/// <summary>Validates the student's 17-digit birth registration number sent in the X-Student-Id header.</summary>
public static class StudentIdentity
{
    public const string HeaderName = "X-Student-Id";

    public static string Require(string? headerValue) =>
        string.IsNullOrWhiteSpace(headerValue)
            ? throw new ValidationException(
                "STUDENT_ID_REQUIRED",
                "Please enter the student's 17-digit birth registration number first.")
            : Validate(headerValue);

    public static string? Optional(string? headerValue) =>
        string.IsNullOrWhiteSpace(headerValue) ? null : Validate(headerValue);

    private static string Validate(string headerValue)
    {
        var value = headerValue.Trim();
        if (value.Length != AdmissionHold.StudentIdLength || !value.All(char.IsAsciiDigit))
        {
            throw new ValidationException(
                "INVALID_STUDENT_ID",
                $"The birth registration number must be exactly {AdmissionHold.StudentIdLength} digits (0-9).");
        }

        return value;
    }
}
