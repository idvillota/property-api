using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Property.Application.Common.Options;
using Property.Application.Features.AiAssist;
using Property.Application.Messaging;
using Property.Domain.Entities;
using Property.Domain.Enums;
using Property.Infrastructure.Services;
using Property.Tests.Unit.Support;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class AiAssistServiceTests
{
    [Fact]
    public async Task AiAssist_ReturnsDraft_DoesNotPersistSend()
    {
        using var fx = new TenantDbFixture();
        var pqrs = new PqrsCase
        {
            Id = Guid.NewGuid(),
            HorizontalPropertyId = Guid.NewGuid(),
            UnitId = Guid.NewGuid(),
            CreatedByPersonId = Guid.NewGuid(),
            Category = PqrsCategory.Complaint,
            Subject = "Ruido en las noches",
            Description = "Vecinos hacen fiesta los viernes.",
            Status = PqrsStatus.Open,
            IsActive = true,
        };
        fx.Db.PqrsCases.Add(pqrs);
        await fx.Db.SaveChangesAsync();

        var sut = new AiAssistService(
            fx.Db,
            Options.Create(new AiAssistOptions { Fail = false }));

        var draft = await sut.SuggestPqrsReplyAsync(pqrs.Id);

        Assert.Contains("Ruido en las noches", draft);
        Assert.Contains("Vecinos hacen fiesta", draft);
        Assert.Equal(0, await fx.Db.OutboxMessages.CountAsync());
        Assert.Equal(0, await fx.Db.NotificationLogs.CountAsync());
    }

    [Fact]
    public async Task AiAssist_FailOption_Throws()
    {
        using var fx = new TenantDbFixture();
        var sut = new AiAssistService(
            fx.Db,
            Options.Create(new AiAssistOptions { Fail = true }));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SuggestDunningTextAsync(new SuggestDunningTextRequestDto
            {
                HorizontalPropertyId = Guid.NewGuid(),
                UnitCode = "101",
                Balance = 100_000m,
                DaysOverdue = 10,
            }));
    }
}
