namespace Property.Application.Common.Interfaces;

public interface IReceptionNotificationService
{
    Task NotifyItemStartedAsync(Guid receptionItemId, CancellationToken cancellationToken = default);
    Task NotifyItemCompletedAsync(Guid receptionItemId, CancellationToken cancellationToken = default);
}
