using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.DTOs.Auth
{
    public record AuthResponseDto(string AccessToken, string RefreshToken);
}
