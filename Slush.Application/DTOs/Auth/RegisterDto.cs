using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Slush.Application.DTOs.Auth
{
    public record RegisterDto(
        [Required(ErrorMessage = "Username is required.")]
        string Username,

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        string Email,

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        string Password,

        [Required(ErrorMessage = "Password confirmation is required.")]
        [property: Compare("Password", ErrorMessage = "Passwords do not match.")]
        string ConfirmPassword
    );
}
