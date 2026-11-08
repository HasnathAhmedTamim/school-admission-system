using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Application.Classes;

namespace SchoolAdmission.API.Controllers;

[ApiController]
[Route("classes")]
[Produces("application/json")]
public sealed class ClassesController(IClassService classService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ClassResponse>> Create(
        [FromBody] CreateClassRequest request,
        CancellationToken cancellationToken)
    {
        var created = await classService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { classId = created.Id }, created);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClassResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await classService.GetAllAsync(cancellationToken));

    [HttpGet("{classId}")]
    public async Task<ActionResult<ClassResponse>> GetById(string classId, CancellationToken cancellationToken) =>
        Ok(await classService.GetByIdAsync(classId, cancellationToken));
}
