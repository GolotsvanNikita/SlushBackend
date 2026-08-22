using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slush.Application.DTOs.Alerts;
using Slush.Application.Interfaces;
using Slush.Domain.Enums;
using System.Threading.Tasks;

namespace Slush.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("channels/{channel}")]
        public async Task<ActionResult<UpdateNotificationChannelRequestDto>> GetChannelConfig(NotificationChannel channel)
        {
            var config = await _notificationService.GetConfigAsync(channel);
            return Ok(config);
        }

        [HttpPut("channels/{channel}")]
        public async Task<ActionResult> UpdateChannelConfig(NotificationChannel channel, [FromBody] UpdateNotificationChannelRequestDto request)
        {
            await _notificationService.UpdateConfigAsync(channel, request);

            return Ok(new
            {
                message = $"The configuration for channel {channel} has been successfully updated.",
                status = request.IsEnabled ? "Active" : "Disabled"
            });
        }
    }
}