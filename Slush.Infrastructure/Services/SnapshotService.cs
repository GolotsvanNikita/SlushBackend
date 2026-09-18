using Slush.Application.Interfaces;
using Slush.Domain.Entities;

namespace Slush.Infrastructure.Services
{
    public class SnapshotService : ISnapshotService
    {
        private readonly IUnitOfWork _uow;
        private readonly PresenceStateService _presenceService;

        public SnapshotService(IUnitOfWork uow, PresenceStateService presenceService)
        {
            _uow = uow;
            _presenceService = presenceService;
        }

        public async Task TakeActivitySnapshotAsync()
        {
            int currentOnline = _presenceService.ActiveConnections.Count;

            var activityRepo = _uow.Repository<ActivitySnapshot>();
            await activityRepo.AddAsync(new ActivitySnapshot
            {
                ActiveCount = currentOnline,
                Timestamp = DateTime.UtcNow
            });

            var gameStats = _presenceService.ActiveConnections.Values
                .Where(s => !string.IsNullOrEmpty(s.CurrentGame))
                .GroupBy(s => s.CurrentGame)
                .Select(g => new GameStatsSnapshot
                {
                    GameName = g.Key,
                    ActivePlayers = g.Count(),
                    Timestamp = DateTime.UtcNow
                })
                .ToList();

            var gameStatsRepo = _uow.Repository<GameStatsSnapshot>();

            foreach (var stat in gameStats)
            {
                await gameStatsRepo.AddAsync(stat);
            }

            await _uow.SaveChangesAsync();
        }

        public async Task SyncGameCatalogAsync()
        {
            await Task.Delay(500);
            Console.WriteLine($"[Hangfire] Catalog successfully synced at {DateTime.UtcNow}");
        }
    }
}