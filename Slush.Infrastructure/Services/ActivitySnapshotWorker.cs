using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;

namespace Slush.Infrastructure.Services;

public class ActivitySnapshotWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PresenceStateService _presenceState;

    public ActivitySnapshotWorker(IServiceProvider serviceProvider, PresenceStateService presenceState)
    {
        _serviceProvider = serviceProvider;
        _presenceState = presenceState;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);

            using var scope = _serviceProvider.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var snapshot = new ActivitySnapshot
            {
                Timestamp = DateTime.UtcNow,
                ActiveCount = _presenceState.ActiveConnections.Count
            };

            await uow.Repository<ActivitySnapshot>().AddAsync(snapshot);
            await uow.SaveChangesAsync();
        }
    }
}