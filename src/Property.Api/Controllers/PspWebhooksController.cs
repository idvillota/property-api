using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Property.Application.Common.Interfaces;

namespace Property.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/webhooks")]
public sealed class PspWebhooksController : ControllerBase
{
    private readonly IPspWebhookService _webhooks;

    public PspWebhooksController(IPspWebhookService webhooks) => _webhooks = webhooks;

    /// <summary>BIL-011 — Wompi signed events (transaction.updated).</summary>
    [HttpPost("wompi")]
    public async Task<IActionResult> Wompi(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var checksum = Request.Headers["X-Event-Checksum"].FirstOrDefault();

        try
        {
            await _webhooks.ProcessWompiEventAsync(body, checksum, cancellationToken);
            return Ok();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
