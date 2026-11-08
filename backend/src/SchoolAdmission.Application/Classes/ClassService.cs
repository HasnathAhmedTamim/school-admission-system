using FluentValidation;
using SchoolAdmission.Application.Common;
using SchoolAdmission.Application.Common.Exceptions;
using SchoolAdmission.Core.Entities;

namespace SchoolAdmission.Application.Classes;

public interface IClassService
{
    Task<ClassResponse> CreateAsync(CreateClassRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<ClassResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<ClassResponse> GetByIdAsync(string classId, CancellationToken cancellationToken);
}

public sealed class ClassService(
    IClassRepository classes,
    IValidator<CreateClassRequest> validator,
    TimeProvider clock) : IClassService
{
    public async Task<ClassResponse> CreateAsync(CreateClassRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateOrThrowAsync(request, cancellationToken);

        var name = request.Name!.Trim();
        if (await classes.NameExistsAsync(SchoolClass.Normalize(name), cancellationToken))
        {
            throw DuplicateName(name);
        }

        var schoolClass = new SchoolClass(name, request.TotalSeats!.Value, clock.GetUtcNow().UtcDateTime);
        await classes.AddAsync(schoolClass, cancellationToken);

        return new ClassSeatSummary(schoolClass.Id, schoolClass.Name, schoolClass.TotalSeats, 0, 0, schoolClass.CreatedAt)
            .ToResponse();
    }

    public async Task<IReadOnlyList<ClassResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var summaries = await classes.GetSummariesAsync(clock.GetUtcNow().UtcDateTime, cancellationToken);
        return summaries.Select(s => s.ToResponse()).ToList();
    }

    public async Task<ClassResponse> GetByIdAsync(string classId, CancellationToken cancellationToken)
    {
        var id = Identifiers.ParseClassId(classId);
        var summary = await classes.GetSummaryAsync(id, clock.GetUtcNow().UtcDateTime, cancellationToken)
            ?? throw Identifiers.ClassNotFound();

        return summary.ToResponse();
    }

    public static ConflictException DuplicateName(string name) =>
        new("CLASS_NAME_TAKEN", $"A class named \"{name}\" already exists. Please choose a different name.");
}
