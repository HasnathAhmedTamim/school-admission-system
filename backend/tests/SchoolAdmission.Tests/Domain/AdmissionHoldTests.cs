using SchoolAdmission.Core.Entities;
using SchoolAdmission.Core.Enums;

namespace SchoolAdmission.Tests.Domain;

public class AdmissionHoldTests
{
    private static readonly DateTime Now = new(2026, 11, 1, 4, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Duration = TimeSpan.FromMinutes(10);

    private static AdmissionHold NewHold() => AdmissionHold.Create(Guid.NewGuid(), "20152691234567890", Now, Duration);

    [Fact]
    public void Create_SetsHeldStatusAndExpiry()
    {
        var hold = NewHold();

        Assert.Equal(HoldStatus.Held, hold.Status);
        Assert.Equal(Now.Add(Duration), hold.ExpiresAt);
        Assert.True(hold.IsActive(Now));
    }

    [Fact]
    public void Confirm_BeforeExpiry_Confirms_AndRepeatIsIdempotent()
    {
        var hold = NewHold();

        Assert.Equal(HoldTransitionResult.Success, hold.Confirm(Now.AddMinutes(5)));
        var confirmedAt = hold.ConfirmedAt;

        Assert.Equal(HoldTransitionResult.AlreadyConfirmed, hold.Confirm(Now.AddMinutes(6)));
        Assert.Equal(HoldStatus.Confirmed, hold.Status);
        Assert.Equal(confirmedAt, hold.ConfirmedAt);
    }

    [Fact]
    public void Confirm_AtOrAfterExpiry_MarksExpired()
    {
        var hold = NewHold();

        Assert.Equal(HoldTransitionResult.Expired, hold.Confirm(hold.ExpiresAt));
        Assert.Equal(HoldStatus.Expired, hold.Status);
        Assert.Null(hold.ConfirmedAt);
    }

    [Fact]
    public void EffectiveStatus_ReportsExpiredOncePastDue_WithoutMutating()
    {
        var hold = NewHold();

        Assert.Equal(HoldStatus.Held, hold.EffectiveStatus(Now.AddMinutes(9)));
        Assert.Equal(HoldStatus.Expired, hold.EffectiveStatus(Now.AddMinutes(10)));
        Assert.Equal(HoldStatus.Held, hold.Status);
    }

    [Fact]
    public void Cancel_ConfirmedHold_IsRejected()
    {
        var hold = NewHold();
        hold.Confirm(Now);

        Assert.Equal(HoldTransitionResult.AlreadyConfirmed, hold.Cancel(Now));
        Assert.Equal(HoldStatus.Confirmed, hold.Status);
    }

    [Fact]
    public void Cancel_ThenConfirm_IsRejected()
    {
        var hold = NewHold();

        Assert.Equal(HoldTransitionResult.Success, hold.Cancel(Now));
        Assert.Equal(HoldTransitionResult.AlreadyCancelled, hold.Cancel(Now));
        Assert.Equal(HoldTransitionResult.AlreadyCancelled, hold.Confirm(Now));
        Assert.Equal(HoldStatus.Cancelled, hold.Status);
    }
}
