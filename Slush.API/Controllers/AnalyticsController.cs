using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slush.Application.DTOs.Analytics;
using Slush.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Slush.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "SuperAdmin, Admin, Analyst")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly IReportService _reportService;

        public AnalyticsController(IAnalyticsService analyticsService, IReportService reportService)
        {
            _analyticsService = analyticsService;
            _reportService = reportService;
        }

        [HttpGet("retention")]
        public async Task<ActionResult<IEnumerable<RetentionPointDto>>> GetRetention()
        {
            return Ok(await _analyticsService.GetRetentionAsync());
        }

        [HttpGet("churn-risk")]
        public async Task<ActionResult<IEnumerable<ChurnRiskDto>>> GetChurnRisk([FromQuery] int thresholdDays = 14)
        {
            return Ok(await _analyticsService.GetChurnRiskAsync(thresholdDays));
        }

        [HttpGet("cohorts")]
        public async Task<ActionResult<IEnumerable<CohortRowDto>>> GetCohorts()
        {
            return Ok(await _analyticsService.GetCohortsAsync());
        }

        [HttpGet("compare")]
        public async Task<ActionResult<PeriodComparisonDto>> ComparePeriods(
            [FromQuery] DateTime fromA,
            [FromQuery] DateTime toA,
            [FromQuery] DateTime fromB,
            [FromQuery] DateTime toB)
        {
            return Ok(await _analyticsService.GetComparisonAsync(fromA, toA, fromB, toB));
        }

        [HttpPost("export")]
        public async Task<IActionResult> ExportReport([FromBody] ExportReportRequestDto request)
        {
            var fileBytes = await _reportService.GenerateReportAsync(request);

            string contentType = request.Format == ReportFormat.Pdf
                ? "application/pdf"
                : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            string fileExtension = request.Format.ToString().ToLower();

            string fileName = $"{request.ReportType}_{DateTime.UtcNow:yyyyMMdd}.{fileExtension}";

            return File(fileBytes, contentType, fileName);
        }
    }
}