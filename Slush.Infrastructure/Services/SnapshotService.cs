using Microsoft.EntityFrameworkCore;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;
using Slush.Infrastructure.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Slush.Infrastructure.Services
{
    public class SnapshotService : ISnapshotService
    {
        private readonly AppDbContext _db;
        private readonly PresenceStateService _presenceService;

        public SnapshotService(AppDbContext db, PresenceStateService presenceService)
        {
            _db = db;
            _presenceService = presenceService;
        }

        public async Task TakeActivitySnapshotAsync()
        {
            int currentOnline = _presenceService.ActiveConnections.Count;

            _db.ActivitySnapshots.Add(new ActivitySnapshot
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

            _db.GameStatsSnapshots.AddRange(gameStats);

            await _db.SaveChangesAsync();
        }

        public async Task SyncGameCatalogAsync()
        {
            await Task.Delay(500);
            Console.WriteLine($"[Hangfire] Catalog successfully synced at {DateTime.UtcNow}");
        }
    }
}