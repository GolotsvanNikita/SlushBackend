using Slush.Application.Interfaces;

namespace Slush.API.BackgroundServices;

public class BadgeBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public BadgeBackgroundWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var badgeService = scope.ServiceProvider.GetRequiredService<IBadgeService>();

            try
            {
                await badgeService.CheckAccountAgeBadgesAsync();
            }
            catch (Exception ex)
            {
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}