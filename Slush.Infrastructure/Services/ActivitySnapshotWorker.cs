using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Slush.Domain.Entities;
using Slush.Infrastructure.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

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
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var snapshot = new ActivitySnapshot
            {
                Timestamp = DateTime.UtcNow,
                ActiveCount = _presenceState.ActiveConnections.Count
            };

            db.ActivitySnapshots.Add(snapshot);
            await db.SaveChangesAsync(stoppingToken);
        }
    }
}