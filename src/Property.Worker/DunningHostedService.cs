using Microsoft.Extensions.Options;
using Property.Application.Common.Interfaces;
using Property.Application.Common.Options;

namespace Property.Worker;

public sealed class DunningHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DunningOptions _options;
    private readonly ILogger<DunningHostedService> _logger;

    public DunningHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<DunningOptions> options,
        ILogger<DunningHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(60, _options.PollIntervalSeconds));
        _logger.LogInformation("Dunning worker started. Poll every {Interval}s", interval.TotalSeconds);

        // Stagger first run slightly so outbox can start first.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService<IDunningJobService>();
                var count = await job.RunAsync(cancellationToken: stoppingToken);
                if (count > 0)
                    _logger.LogInformation("Dunning run enqueued {Count} reminder(s)", count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dunning job loop failed");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
