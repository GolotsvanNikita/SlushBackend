using Microsoft.AspNetCore.SignalR;
using Slush.API.Hubs;
using Slush.Application.Interfaces;

namespace Slush.API.BackgroundServices;

public class StaleGameSessionCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StaleGameSessionCleanupService> _logger;

    public StaleGameSessionCleanupService(
        IServiceScopeFactory scopeFactory,
        IHubContext<ChatHub> hubContext,
        IConfiguration configuration,
        ILogger<StaleGameSessionCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                int timeoutMinutes = _configuration.GetValue<int>("GameSession:StaleTimeoutMinutes", 3);
                var staleTimeout = TimeSpan.FromMinutes(Math.Max(1, timeoutMinutes));

                using var scope = _scopeFactory.CreateScope();
                var libraryService = scope.ServiceProvider.GetRequiredService<ILibraryService>();

                var closedSessions = await libraryService.CleanupStaleSessionsAsync(staleTimeout);

                foreach (var (toast, friendIds) in closedSessions)
                {
                    if (friendIds.Any())
                    {
                        var groups = friendIds.Select(id => $"user_{id}").ToList();
                        await _hubContext.Clients.Groups(groups).SendAsync("FriendPresenceChanged", toast, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while cleaning up stale game sessions.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}