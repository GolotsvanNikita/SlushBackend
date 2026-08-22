using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Slush.Application.DTOs.Auth
{
    public record ResetPasswordDto(
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        string Email,

        [Required(ErrorMessage = "Verification code is required.")]
        string Code,

        [Required(ErrorMessage = "New password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        string NewPassword,

        [Required(ErrorMessage = "Password confirmation is required.")]
        [property: Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        string ConfirmPassword
    );
}
