using Microsoft.AspNetCore.Mvc;
using Slush.Application.DTOs.Monitoring;
using Slush.Infrastructure.Services;

namespace Slush.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MonitoringController : ControllerBase
{
    private readonly PresenceStateService _state;

    public MonitoringController(PresenceStateService state)
    {
        _state = state;
    }

    [HttpGet("summary")]
    public IActionResult GetSummary()
    {
        var activeSessions = _state.ActiveConnections.Count;

        var summary = new DashboardSummaryDto(
            TotalMembers: 48304,
            OnlineMembers: 43855 + activeSessions,
            ActiveSessionsCount: 19740 + activeSessions,
            NetworkStabilityPercent: 99.9,
            ActiveAlertsCount: 3
        );

        return Ok(summary);
    }

    [HttpGet("online")]
    public IActionResult GetOnlineUsers()
    {
        var mockData = new List<OnlineStatusDto>
        {
            new(
                SteamId: "76561198000000001",
                Nickname: "Commander_Alex",
                Status: "InGame",
                CurrentGame: "Counter-Strike 2",
                PlaytimeHours: 1420.5,
                Lat: 55.7558,
                Lng: 37.6173,
                City: "Odesa",
                Country: "UA"
            )
        };

        return Ok(mockData);
    }
}