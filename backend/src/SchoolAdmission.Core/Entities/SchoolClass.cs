namespace SchoolAdmission.Core.Entities;

/// <summary>
/// A single admission unit. Each class/shift combination (e.g. "Class 6 - Morning Shift")
/// is its own SchoolClass with its own seat capacity.
/// </summary>
public class SchoolClass
{
    public const int NameMaxLength = 100;
    public const int MinSeats = 1;
    public const int MaxSeats = 10_000;

    private SchoolClass()
    {
    }

    public SchoolClass(string name, int totalSeats, DateTime createdAtUtc)
    {
        Id = Guid.CreateVersion7();
        Name = name.Trim();
        NormalizedName = Normalize(name);
        TotalSeats = totalSeats;
        CreatedAt = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    /// <summary>Upper-cased, trimmed name used to enforce case-insensitive uniqueness.</summary>
    public string NormalizedName { get; private set; } = null!;

    public int TotalSeats { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public ICollection<AdmissionHold> Holds { get; private set; } = new List<AdmissionHold>();

    public static string Normalize(string name) => name.Trim().ToUpperInvariant();
}
