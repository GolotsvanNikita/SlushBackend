using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.DTOs.Auth
{
    public record RegisterDto(string Username, string Email, string Password);
}
