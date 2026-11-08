using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolAdmission.Core.Entities;

namespace SchoolAdmission.Infrastructure.Persistence.Configurations;

public class SchoolClassConfiguration : IEntityTypeConfiguration<SchoolClass>
{
    public void Configure(EntityTypeBuilder<SchoolClass> builder)
    {
        builder.ToTable("classes", t =>
            t.HasCheckConstraint(
                "ck_classes_total_seats_range",
                $"total_seats BETWEEN {SchoolClass.MinSeats} AND {SchoolClass.MaxSeats}"));

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(SchoolClass.NameMaxLength);

        builder.Property(c => c.NormalizedName)
            .IsRequired()
            .HasMaxLength(SchoolClass.NameMaxLength);

        builder.HasIndex(c => c.NormalizedName)
            .IsUnique()
            .HasDatabaseName("ux_classes_normalized_name");

        builder.Property(c => c.TotalSeats).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
    }
}
