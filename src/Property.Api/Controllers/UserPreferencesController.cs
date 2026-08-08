using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Property.Application.Common.Interfaces;
using Property.Application.Features.UserPreferences;

namespace Property.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/user/preferences")]
public sealed class UserPreferencesController : ControllerBase
{
    private readonly IUserPreferencesService _service;

    public UserPreferencesController(IUserPreferencesService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<UserPreferencesDto>> Get(CancellationToken cancellationToken = default) =>
        Ok(await _service.GetAsync(cancellationToken));

    [HttpPut]
    public async Task<ActionResult<UserPreferencesDto>> Update(
        [FromBody] UpdateUserPreferencesDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            return Ok(await _service.UpdateAsync(dto, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
