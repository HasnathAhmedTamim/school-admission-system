using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolAdmission.Core.Entities;
using SchoolAdmission.Core.Enums;

namespace SchoolAdmission.Infrastructure.Persistence.Configurations;

public class AdmissionHoldConfiguration : IEntityTypeConfiguration<AdmissionHold>
{
    public const string ActiveStudentIndexName = "ux_admission_holds_active_student";

    public void Configure(EntityTypeBuilder<AdmissionHold> builder)
    {
        builder.ToTable("admission_holds", t =>
        {
            t.HasCheckConstraint(
                "ck_admission_holds_student_id_format",
                $"student_id ~ '^[0-9]{{{AdmissionHold.StudentIdLength}}}$'");
            t.HasCheckConstraint(
                "ck_admission_holds_status",
                "status IN ('HELD', 'CONFIRMED', 'EXPIRED', 'CANCELLED')");
            t.HasCheckConstraint(
                "ck_admission_holds_expiry_after_creation",
                "expires_at > created_at");
            t.HasCheckConstraint(
                "ck_admission_holds_confirmed_at",
                "(status = 'CONFIRMED') = (confirmed_at IS NOT NULL)");
            t.HasCheckConstraint(
                "ck_admission_holds_cancelled_at",
                "(status = 'CANCELLED') = (cancelled_at IS NOT NULL)");
        });

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.StudentId)
            .IsRequired()
            .HasMaxLength(AdmissionHold.StudentIdLength)
            .IsFixedLength();

        builder.Property(h => h.Status)
            .IsRequired()
            .HasMaxLength(16)
            .HasConversion(
                status => status.ToString().ToUpperInvariant(),
                value => Enum.Parse<HoldStatus>(value, true));

        builder.Property(h => h.CreatedAt).IsRequired();
        builder.Property(h => h.ExpiresAt).IsRequired();

        builder.HasOne(h => h.Class)
            .WithMany(c => c.Holds)
            .HasForeignKey(h => h.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        // A student may hold at most one active (HELD) or CONFIRMED seat per class.
        builder.HasIndex(h => new { h.ClassId, h.StudentId })
            .IsUnique()
            .HasFilter("status IN ('HELD', 'CONFIRMED')")
            .HasDatabaseName(ActiveStudentIndexName);

        builder.HasIndex(h => new { h.ClassId, h.Status, h.ExpiresAt })
            .HasDatabaseName("ix_admission_holds_class_status_expires");

        builder.HasIndex(h => new { h.StudentId, h.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_admission_holds_student_created");

        builder.HasIndex(h => h.ExpiresAt)
            .HasFilter("status = 'HELD'")
            .HasDatabaseName("ix_admission_holds_held_expires");
    }
}
