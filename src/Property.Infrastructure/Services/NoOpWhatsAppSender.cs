using Microsoft.Extensions.Logging;
using Property.Application.Common.Interfaces;

namespace Property.Infrastructure.Services;

public sealed class NoOpWhatsAppSender : IWhatsAppSender
{
    private readonly ILogger<NoOpWhatsAppSender> _logger;

    public NoOpWhatsAppSender(ILogger<NoOpWhatsAppSender> logger) => _logger = logger;

    public Task SendAsync(
        string toPhone,
        string body,
        string? templateKey = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "WhatsApp (noop) to {Phone} template={Template}: {BodyPreview}",
            toPhone,
            templateKey ?? "(none)",
            body.Length > 80 ? body[..80] + "…" : body);
        return Task.CompletedTask;
    }
}
