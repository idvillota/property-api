using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Webhooks;

namespace Property.Api.Controllers;

[ApiController]
[RequireFeature(FeatureCodes.PlatformApi)]
[Route("api/settings/webhooks")]
public sealed class WebhooksController : ControllerBase
{
    private readonly IWebhookSubscriptionService _service;

    public WebhooksController(IWebhookSubscriptionService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WebhookSubscriptionDto>>> List(
        CancellationToken cancellationToken = default) =>
        Ok(await _service.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WebhookSubscriptionDto>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var dto = await _service.GetByIdAsync(id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<WebhookSubscriptionDto>> Create(
        [FromBody] CreateWebhookSubscriptionDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var created = await _service.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WebhookSubscriptionDto>> Update(
        Guid id,
        [FromBody] UpdateWebhookSubscriptionDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var updated = await _service.UpdateAsync(id, dto, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var ok = await _service.DeleteAsync(id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}
