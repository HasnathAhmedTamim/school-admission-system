namespace SchoolAdmission.Application.Admissions;

public sealed class HoldSettings
{
    public const int DefaultDurationSeconds = 600;

    public HoldSettings(int durationSeconds)
    {
        if (durationSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Hold duration must be a positive number of seconds.");
        }

        Duration = TimeSpan.FromSeconds(durationSeconds);
    }

    public TimeSpan Duration { get; }

    /// <summary>Parses the HOLD_DURATION_SECONDS setting; an unset value falls back to the default.</summary>
    public static HoldSettings FromSetting(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return new HoldSettings(DefaultDurationSeconds);
        }

        if (!int.TryParse(rawValue, out var seconds) || seconds <= 0)
        {
            throw new InvalidOperationException(
                $"HOLD_DURATION_SECONDS must be a positive whole number of seconds, but was '{rawValue}'.");
        }

        return new HoldSettings(seconds);
    }
}
