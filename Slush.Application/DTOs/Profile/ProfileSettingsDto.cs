using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.DTOs.Profile
{
    public class ProfileSettingsDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string CoverUrl { get; set; } = string.Empty;
    }
}
