using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.DTOs.Admin
{
    public record UserListDto(
        Guid Id,
        string Username,
        string Email,
        bool IsBanned,
        string Role,
        DateTime CreatedAt
    );
}
