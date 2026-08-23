using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Platform.Files;

namespace Property.Api.Controllers;

/// <summary>
/// Platform file upload/download (ARCH-004). Feature modules may also call <see cref="IFileAssetService"/> under their own permissions.
/// </summary>
[ApiController]
[Authorize]
[Route("api/files")]
public sealed class FilesController : ControllerBase
{
    private readonly IFileAssetService _files;

    public FilesController(IFileAssetService files) => _files = files;

    [HttpPost]
    [RequestSizeLimit(52_428_800)] // hard ceiling; service enforces FileStorage:MaxBytes
    public async Task<ActionResult<FileAssetDto>> Upload(
        IFormFile file,
        [FromForm] string? purpose,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "File is required." });

        try
        {
            await using var stream = file.OpenReadStream();
            var asset = await _files.UploadAsync(
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                purpose,
                cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = asset.Id }, asset);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FileAssetDto>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var asset = await _files.GetByIdAsync(id, cancellationToken);
        return asset is null ? NotFound() : Ok(asset);
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _files.DownloadAsync(id, cancellationToken);
        if (result is null)
            return NotFound();

        return File(result.Content, result.Asset.ContentType, result.Asset.FileName);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken cancellationToken = default)
    {
        var ok = await _files.SoftDeleteAsync(id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}
