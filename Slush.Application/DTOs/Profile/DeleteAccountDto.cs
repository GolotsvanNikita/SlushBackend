using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.DTOs.Profile
{
    public class DeleteAccountDto
    {
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
