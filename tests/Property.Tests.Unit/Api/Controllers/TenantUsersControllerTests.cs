using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Property.Api.Controllers;
using Property.Application.Common.Interfaces;
using Property.Application.Features.Organization.TenantUsers;

namespace Property.Tests.Unit.Api.Controllers;

public sealed class TenantUsersControllerTests
{
    private readonly Mock<ITenantUserInviteService> _service = new();
    private readonly TenantUsersController _controller;

    public TenantUsersControllerTests() => _controller = new TenantUsersController(_service.Object);

    [Fact]
    public async Task List_returns_ok()
    {
        var items = new List<TenantUserListItemDto>
        {
            new()
            {
                TenantUserId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Email = "admin@example.com",
                Roles = ["Administrator"],
                IsActive = true,
            },
        };
        _service.Setup(s => s.ListAsync(default)).ReturnsAsync(items);

        var result = await _controller.List();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsAssignableFrom<IReadOnlyList<TenantUserListItemDto>>(ok.Value);
        Assert.Single(body);
        Assert.Equal("admin@example.com", body[0].Email);
    }

    [Fact]
    public async Task Invite_returns_201_when_successful()
    {
        var dto = new InvitedTenantUserDto
        {
            UserId = Guid.NewGuid(),
            TenantUserId = Guid.NewGuid(),
            Email = "manager@example.com",
            Roles = new[] { "Manager" },
        };
        _service.Setup(s => s.InviteAsync(It.IsAny<InviteTenantUserDto>(), default)).ReturnsAsync(dto);

        var result = await _controller.Invite(new InviteTenantUserDto
        {
            Email = "manager@example.com",
            Password = "Str0ng!Pass",
            Role = "Manager",
        });

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        var body = Assert.IsType<InvitedTenantUserDto>(objectResult.Value);
        Assert.Equal("manager@example.com", body.Email);
    }

    [Fact]
    public async Task Invite_returns_conflict_when_already_member()
    {
        _service.Setup(s => s.InviteAsync(It.IsAny<InviteTenantUserDto>(), default))
            .ThrowsAsync(new InvalidOperationException("This user is already a member of this tenant."));

        var result = await _controller.Invite(new InviteTenantUserDto
        {
            Email = "dup@example.com",
            Password = "Str0ng!Pass",
            Role = "Manager",
        });

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Invite_returns_bad_request_on_validation_error_from_service()
    {
        _service.Setup(s => s.InviteAsync(It.IsAny<InviteTenantUserDto>(), default))
            .ThrowsAsync(new InvalidOperationException("Password is required for new users."));

        var result = await _controller.Invite(new InviteTenantUserDto
        {
            Email = "new@example.com",
            Role = "Manager",
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
