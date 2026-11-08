using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Application.Admissions;

namespace SchoolAdmission.API.Controllers;

[ApiController]
[Route("me")]
[Produces("application/json")]
public sealed class MeController(IAdmissionService admissionService) : ControllerBase
{
    [HttpGet("holds")]
    public async Task<ActionResult<IReadOnlyList<HoldResponse>>> GetMyHolds(
        [FromHeader(Name = StudentIdentity.HeaderName)] string? studentId,
        CancellationToken cancellationToken) =>
        Ok(await admissionService.GetMyHoldsAsync(studentId, cancellationToken));
}
