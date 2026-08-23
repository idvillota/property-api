using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Models;
using Property.Application.Features.Billing.Payments;

namespace Property.Api.Controllers;

[ApiController]
[RequireFeature(FeatureCodes.BillingPayments)]
[Route("api/[controller]")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _service;
    private readonly IPaymentReceiptService _receipts;

    public PaymentsController(IPaymentService service, IPaymentReceiptService receipts)
    {
        _service = service;
        _receipts = receipts;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PaymentDto>>> List(
        [FromQuery] ListQuery query,
        CancellationToken cancellationToken = default) =>
        Ok(await _service.ListAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("{id:guid}/receipt")]
    public async Task<IActionResult> DownloadReceipt(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _receipts.DownloadReceiptAsync(id, cancellationToken);
            if (result is null)
                return NotFound();
            return File(result.Content, result.Asset.ContentType, result.Asset.FileName);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Create(
        [FromBody] CreatePaymentDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var created = await _service.CreateAsync(dto, cancellationToken);
            await _receipts.EnsureReceiptAsync(created.Id, enqueueEmailIfEnabled: true, cancellationToken);
            var withReceipt = await _service.GetByIdAsync(created.Id, cancellationToken) ?? created;
            return CreatedAtAction(nameof(GetById), new { id = withReceipt.Id }, withReceipt);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var ok = await _service.SoftDeleteAsync(id, cancellationToken);
            return ok ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
