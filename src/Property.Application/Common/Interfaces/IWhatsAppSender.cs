namespace Property.Application.Common.Interfaces;

public interface IWhatsAppSender
{
    Task SendAsync(
        string toPhone,
        string body,
        string? templateKey = null,
        CancellationToken cancellationToken = default);
}
