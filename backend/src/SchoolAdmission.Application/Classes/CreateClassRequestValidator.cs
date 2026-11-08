using FluentValidation;
using SchoolAdmission.Core.Entities;

namespace SchoolAdmission.Application.Classes;

public sealed class CreateClassRequestValidator : AbstractValidator<CreateClassRequest>
{
    public CreateClassRequestValidator()
    {
        RuleFor(r => r.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Class name is required.")
            .Must(name => name is null || name.Trim().Length <= SchoolClass.NameMaxLength)
            .WithMessage($"Class name must be at most {SchoolClass.NameMaxLength} characters.");

        RuleFor(r => r.TotalSeats)
            .NotNull()
            .WithMessage("Total seats is required.")
            .InclusiveBetween(SchoolClass.MinSeats, SchoolClass.MaxSeats)
            .WithMessage($"Total seats must be a whole number between {SchoolClass.MinSeats} and {SchoolClass.MaxSeats}.");
    }
}
