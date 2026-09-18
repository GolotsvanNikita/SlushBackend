using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Monitoring;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;
using Slush.Infrastructure.Services;

namespace Slush.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin, Admin, Analyst")]
public class MonitoringController : ControllerBase
{
    private readonly PresenceStateService _state;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public MonitoringController(PresenceStateService state, IUnitOfWork uow, IMapper mapper)
    {
        _state = state;
        _uow = uow;
        _mapper = mapper;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var totalMembers = await _uow.Repository<User>().AsQueryable().CountAsync();
        var activeAlertsCount = await _uow.Repository<AlertHistory>().AsQueryable().CountAsync(a => !a.IsRead);
        var activeSessions = _state.ActiveConnections.Count;

        var summary = new DashboardSummaryDto(
            TotalMembers: totalMembers,
            OnlineMembers: activeSessions,
            ActiveSessionsCount: activeSessions,
            NetworkStabilityPercent: 99.9,
            ActiveAlertsCount: activeAlertsCount
        );

        return Ok(summary);
    }

    [HttpGet("online")]
    public IActionResult GetOnlineUsers()
    {
        var onlineUsers = _state.ActiveConnections.Values.Select(s => new OnlineStatusDto(
            SteamId: s.UserId,
            Nickname: s.Nickname,
            Status: s.Status,
            CurrentGame: s.CurrentGame,
            PlaytimeHours: 0,
            Lat: s.Location.Lat,
            Lng: s.Location.Lng,
            City: s.Location.City,
            Country: s.Location.Country
        )).ToList();

        return Ok(onlineUsers);
    }

    [HttpGet("activity-history")]
    public async Task<ActionResult<IEnumerable<ActivityPointDto>>> GetActivityHistory([FromQuery] string period = "day")
    {
        var cutoff = period.ToLower() switch
        {
            "week" => DateTime.UtcNow.AddDays(-7),
            "month" => DateTime.UtcNow.AddMonths(-1),
            _ => DateTime.UtcNow.AddDays(-1)
        };

        var history = await _uow.Repository<ActivitySnapshot>().AsQueryable()
            .Where(s => s.Timestamp >= cutoff)
            .OrderBy(s => s.Timestamp)
            .ToListAsync();

        var historyDto = _mapper.Map<IEnumerable<ActivityPointDto>>(history);
        return Ok(historyDto);
    }

    [HttpGet("heatmap")]
    public async Task<ActionResult<IEnumerable<HeatmapPointDto>>> GetHeatmap()
    {
        var snapshots = await _uow.Repository<ActivitySnapshot>().AsQueryable()
            .Where(s => s.Timestamp >= DateTime.UtcNow.AddDays(-7))
            .ToListAsync();

        if (!snapshots.Any())
            return Ok(new List<HeatmapPointDto>());

        var maxActivity = snapshots.Max(s => s.ActiveCount);
        if (maxActivity == 0) maxActivity = 1;

        var heatmap = snapshots
            .GroupBy(s => new { Day = s.Timestamp.DayOfWeek, Hour = s.Timestamp.Hour })
            .Select(g => new HeatmapPointDto(
                (int)g.Key.Day,
                g.Key.Hour,
                Math.Round((double)g.Average(s => s.ActiveCount) / maxActivity, 2)
            ))
            .ToList();

        return Ok(heatmap);
    }

    [HttpGet("game-trends")]
    public IActionResult GetGameTrends()
    {
        var trends = _state.ActiveConnections.Values
            .Where(s => !string.IsNullOrEmpty(s.CurrentGame))
            .GroupBy(s => s.CurrentGame)
            .Select(g => new GameTrendPointDto(g.Key, g.Count()))
            .OrderByDescending(t => t.PlayerCount)
            .Take(5)
            .ToList();

        return Ok(trends);
    }
}