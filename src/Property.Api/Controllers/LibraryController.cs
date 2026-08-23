using Microsoft.AspNetCore.Mvc;
using Property.Api.Authorization;
using Property.Application.Authorization;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Community;

namespace Property.Api.Controllers;

[ApiController]
[Route("api/library")]
public sealed class LibraryController : ControllerBase
{
    private readonly ILibraryService _library;

    public LibraryController(ILibraryService library) => _library = library;

    [HttpGet]
    [RequireFeature(FeatureCodes.DocsLibrary)]
    public async Task<ActionResult<IReadOnlyList<LibraryDocumentDto>>> ListAdmin(
        [FromQuery] Guid horizontalPropertyId,
        CancellationToken cancellationToken = default) =>
        Ok(await _library.ListAdminAsync(horizontalPropertyId, cancellationToken));

    [HttpPost]
    [RequireFeature(FeatureCodes.DocsLibrary)]
    public async Task<ActionResult<LibraryDocumentDto>> Create(
        [FromBody] CreateLibraryDocumentDto dto,
        CancellationToken cancellationToken = default) =>
        Ok(await _library.CreateAsync(dto, cancellationToken));

    [HttpGet("portal")]
    [RequireFeature(FeatureCodes.PortalLibrary)]
    public async Task<ActionResult<IReadOnlyList<LibraryDocumentDto>>> ListPortal(
        [FromQuery] Guid horizontalPropertyId,
        [FromQuery] bool isOwner = true,
        CancellationToken cancellationToken = default) =>
        Ok(await _library.ListPortalAsync(horizontalPropertyId, isOwner, cancellationToken));
}
