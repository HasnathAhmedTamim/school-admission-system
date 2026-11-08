using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Core.Entities;

namespace SchoolAdmission.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<SchoolClass> Classes => Set<SchoolClass>();

    public DbSet<AdmissionHold> AdmissionHolds => Set<AdmissionHold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
