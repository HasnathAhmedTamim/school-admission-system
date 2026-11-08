using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Application.Admissions;

namespace SchoolAdmission.API.Controllers;

[ApiController]
[Route("holds")]
[Produces("application/json")]
public sealed class HoldsController(IAdmissionService admissionService) : ControllerBase
{
    [HttpGet("{holdId}")]
    public async Task<ActionResult<HoldResponse>> GetById(
        string holdId,
        [FromHeader(Name = StudentIdentity.HeaderName)] string? studentId,
        CancellationToken cancellationToken) =>
        Ok(await admissionService.GetHoldAsync(holdId, studentId, cancellationToken));

    [HttpPost("{holdId}/confirm")]
    public async Task<ActionResult<ConfirmHoldResponse>> Confirm(
        string holdId,
        [FromHeader(Name = StudentIdentity.HeaderName)] string? studentId,
        CancellationToken cancellationToken) =>
        Ok(await admissionService.ConfirmAsync(holdId, studentId, cancellationToken));

    [HttpDelete("{holdId}")]
    public async Task<IActionResult> Cancel(
        string holdId,
        [FromHeader(Name = StudentIdentity.HeaderName)] string? studentId,
        CancellationToken cancellationToken)
    {
        await admissionService.CancelAsync(holdId, studentId, cancellationToken);
        return NoContent();
    }
}
