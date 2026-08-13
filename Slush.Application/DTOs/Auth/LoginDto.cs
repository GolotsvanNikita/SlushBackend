using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.DTOs.Auth
{
    public record LoginDto(string LoginOrEmail, string Password, bool RememberMe);
}
