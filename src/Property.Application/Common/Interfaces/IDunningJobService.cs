namespace Property.Application.Common.Interfaces;

public interface IDunningJobService
{
    /// <summary>
    /// Cross-tenant: evaluate enabled rules, enqueue reminder emails, write send logs.
    /// Returns number of reminders enqueued.
    /// </summary>
    Task<int> RunAsync(DateOnly? asOfDate = null, CancellationToken cancellationToken = default);
}
