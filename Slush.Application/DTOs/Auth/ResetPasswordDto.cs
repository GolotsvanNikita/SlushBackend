using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.DTOs.Auth
{
    public record ResetPasswordDto(string Email, string Code, string NewPassword);
}
