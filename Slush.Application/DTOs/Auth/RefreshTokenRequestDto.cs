using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Slush.Application.DTOs.Auth
{
    public record RefreshTokenRequestDto
    {
        [Required]
        public string RefreshToken { get; init; } = string.Empty;
    }
}
