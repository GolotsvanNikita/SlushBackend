using Microsoft.EntityFrameworkCore;
using Slush.Application.DTOs.Analytics;
using Slush.Application.Interfaces;
using Slush.Domain.Entities;

namespace Slush.Infrastructure.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IUnitOfWork _uow;

        public AnalyticsService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IEnumerable<RetentionPointDto>> GetRetentionAsync()
        {
            var retentionPoints = new List<RetentionPointDto>();
            var daysToCheck = new[] { 1, 3, 7, 14, 30 };
            var now = DateTime.UtcNow.Date;

            var usersRepo = _uow.Repository<User>().AsQueryable();
            var historyRepo = _uow.Repository<UserLoginHistory>().AsQueryable();

            foreach (var day in daysToCheck)
            {
                var cutoffDate = now.AddDays(-day);

                var eligibleUsers = usersRepo.Where(u => u.CreatedAt.Date <= cutoffDate);
                int eligibleCount = await eligibleUsers.CountAsync();

                if (eligibleCount == 0)
                {
                    retentionPoints.Add(new RetentionPointDto(day, 0));
                    continue;
                }

                int retainedCount = await eligibleUsers
                    .Where(u => historyRepo.Any(h =>
                        h.UserId == u.Id &&
                        h.LoginTimestamp.Date == u.CreatedAt.Date.AddDays(day)))
                    .CountAsync();

                double rate = Math.Round((double)retainedCount / eligibleCount * 100, 2);
                retentionPoints.Add(new RetentionPointDto(day, rate));
            }

            return retentionPoints;
        }

        public async Task<IEnumerable<ChurnRiskDto>> GetChurnRiskAsync(int inactivityThresholdDays = 14)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-inactivityThresholdDays);

            return await _uow.Repository<User>().AsQueryable()
                .Where(u => !u.IsBanned)
                .Where(u => (u.LastLoginAt != null && u.LastLoginAt < cutoffDate) ||
                            (u.LastLoginAt == null && u.CreatedAt < cutoffDate))
                .OrderBy(u => u.LastLoginAt ?? u.CreatedAt)
                .Take(50)
                .Select(u => new ChurnRiskDto(
                    u.Id,
                    u.Username,
                    u.Email,
                    (DateTime.UtcNow - (u.LastLoginAt ?? u.CreatedAt)).Days))
                .ToListAsync();
        }

        public async Task<IEnumerable<CohortRowDto>> GetCohortsAsync()
        {
            var cohorts = new List<CohortRowDto>();
            var usersRepo = _uow.Repository<User>().AsQueryable();
            var historyRepo = _uow.Repository<UserLoginHistory>().AsQueryable();

            var cohortUsers = await usersRepo
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new {
                    g.Key.Year,
                    g.Key.Month,
                    UserIds = g.Select(u => u.Id).ToList()
                })
                .ToListAsync();

            foreach (var cohort in cohortUsers.OrderBy(x => x.Year).ThenBy(x => x.Month))
            {
                string monthStr = $"{cohort.Year}-{cohort.Month:D2}";
                int totalUsers = cohort.UserIds.Count;

                var retentionRates = new List<double> { 100.0 };
                DateTime cohortStart = new DateTime(cohort.Year, cohort.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                int monthsPassed = ((DateTime.UtcNow.Year - cohort.Year) * 12) + DateTime.UtcNow.Month - cohort.Month;

                for (int m = 1; m <= monthsPassed; m++)
                {
                    var targetMonth = cohortStart.AddMonths(m);

                    int activeUsersCount = await historyRepo
                        .Where(h => cohort.UserIds.Contains(h.UserId) &&
                                    h.LoginTimestamp.Year == targetMonth.Year &&
                                    h.LoginTimestamp.Month == targetMonth.Month)
                        .Select(h => h.UserId)
                        .Distinct()
                        .CountAsync();

                    double rate = Math.Round((double)activeUsersCount / totalUsers * 100, 2);
                    retentionRates.Add(rate);
                }

                cohorts.Add(new CohortRowDto(monthStr, totalUsers, retentionRates));
            }

            return cohorts;
        }

        public async Task<PeriodComparisonDto> GetComparisonAsync(DateTime fromA, DateTime toA, DateTime fromB, DateTime toB)
        {
            var usersRepo = _uow.Repository<User>().AsQueryable();
            var snapshotsRepo = _uow.Repository<ActivitySnapshot>().AsQueryable();

            var usersA = await usersRepo.CountAsync(u => u.CreatedAt >= fromA && u.CreatedAt <= toA);
            var usersB = await usersRepo.CountAsync(u => u.CreatedAt >= fromB && u.CreatedAt <= toB);
            double usersGrowth = usersA == 0 ? 0 : Math.Round(((double)(usersB - usersA) / usersA) * 100, 2);

            var activeA = await snapshotsRepo
                .Where(s => s.Timestamp >= fromA && s.Timestamp <= toA)
                .MaxAsync(s => (int?)s.ActiveCount) ?? 0;

            var activeB = await snapshotsRepo
                .Where(s => s.Timestamp >= fromB && s.Timestamp <= toB)
                .MaxAsync(s => (int?)s.ActiveCount) ?? 0;
            double activeGrowth = activeA == 0 ? 0 : Math.Round(((double)(activeB - activeA) / activeA) * 100, 2);

            return new PeriodComparisonDto(usersA, usersB, usersGrowth, activeA, activeB, activeGrowth);
        }
    }
}