using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slush.Application.DTOs.Audit;
using Slush.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace Slush.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin, Admin")]
    public class HealthController : ControllerBase
    {
        private readonly AppDbContext _db;

        public HealthController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("detailed")]
        public async Task<ActionResult<SystemHealthDto>> GetDetailedHealth()
        {
            bool isDbHealthy = await _db.Database.CanConnectAsync();

            bool isExternalApiHealthy = true;

            string overall = (isDbHealthy && isExternalApiHealthy) ? "Healthy" : "Degraded";

            return Ok(new SystemHealthDto(
                isDbHealthy ? "Connected" : "Disconnected",
                isExternalApiHealthy ? "Connected" : "Disconnected",
                overall,
                DateTime.UtcNow
            ));
        }
    }
}