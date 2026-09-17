using Slush.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.DTOs.Auth
{
    public record UpdateUserRoleRequestDto
    {
        public UserRole NewRole { get; init; }
    }
}
