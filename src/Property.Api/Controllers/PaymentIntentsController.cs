using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Billing.PaymentIntents;

namespace Property.Api.Controllers;

[ApiController]
[RequireFeature(FeatureCodes.BillingPayments)]
[Route("api/payment-intents")]
public sealed class PaymentIntentsController : ControllerBase
{
    private readonly IPaymentIntentService _service;

    public PaymentIntentsController(IPaymentIntentService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResult<PaymentIntentDto>>> List(
        [FromQuery] ListQuery query,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.ListAsync(query, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<PaymentIntentDto>> Create(
        [FromBody] CreatePaymentIntentDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var intent = await _service.CreateOrGetAsync(dto, cancellationToken);
            return Ok(intent);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentIntentDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var intent = await _service.GetByIdAsync(id, cancellationToken);
        return intent is null ? NotFound() : Ok(intent);
    }

    /// <summary>Dev helper — prefer Wompi webhook / portal sandbox-confirm in BIL-011.</summary>
    [HttpPost("{id:guid}/mark-paid")]
    public async Task<ActionResult<PaymentIntentDto>> MarkPaid(
        Guid id,
        [FromBody] MarkPaymentIntentPaidDto? dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var intent = await _service.MarkPaidAsync(id, dto ?? new MarkPaymentIntentPaidDto(), cancellationToken);
            return Ok(intent);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/mark-failed")]
    public async Task<ActionResult<PaymentIntentDto>> MarkFailed(
        Guid id,
        [FromBody] MarkPaymentIntentFailedDto? dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var intent = await _service.MarkFailedAsync(id, dto ?? new MarkPaymentIntentFailedDto(), cancellationToken);
            return Ok(intent);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/requires-action")]
    public async Task<ActionResult<PaymentIntentDto>> RequiresAction(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _service.MarkRequiresActionAsync(id, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<PaymentIntentDto>> Cancel(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _service.CancelAsync(id, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
