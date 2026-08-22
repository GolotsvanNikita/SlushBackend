using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slush.Application.DTOs.Alerts;
using Slush.Application.DTOs.Common;
using Slush.Application.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Slush.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AlertsController : ControllerBase
    {
        private readonly IAlertService _alertService;

        public AlertsController(IAlertService alertService)
        {
            _alertService = alertService;
        }

        [HttpGet("rules")]
        public async Task<ActionResult<IEnumerable<AlertRuleDto>>> GetRules()
        {
            return Ok(await _alertService.GetRulesAsync());
        }

        [HttpPost("rules")]
        public async Task<ActionResult<AlertRuleDto>> CreateRule([FromBody] CreateAlertRuleRequestDto request)
        {
            var newRule = await _alertService.CreateRuleAsync(request);
            return CreatedAtAction(nameof(GetRules), new { id = newRule.Id }, newRule);
        }

        [HttpGet("history")]
        public async Task<ActionResult<PagedResultDto<AlertHistoryDto>>> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            return Ok(await _alertService.GetHistoryAsync(page, pageSize));
        }
    }
}