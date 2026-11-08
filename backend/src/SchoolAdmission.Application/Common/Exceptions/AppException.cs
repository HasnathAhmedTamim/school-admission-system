namespace SchoolAdmission.Application.Common.Exceptions;

/// <summary>
/// Base type for expected, client-facing failures. The API maps each subtype to an HTTP status
/// and returns <see cref="ErrorCode"/> and the message to the client.
/// </summary>
public abstract class AppException(string errorCode, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}

public sealed class ValidationException(string errorCode, string message, IReadOnlyDictionary<string, string[]>? errors = null)
    : AppException(errorCode, message)
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors ?? new Dictionary<string, string[]>();
}

public sealed class NotFoundException(string errorCode, string message) : AppException(errorCode, message);

public sealed class ForbiddenException(string errorCode, string message) : AppException(errorCode, message);

public sealed class ConflictException(string errorCode, string message) : AppException(errorCode, message);
