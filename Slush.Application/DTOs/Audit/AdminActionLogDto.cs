using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.DTOs.Audit
{
    public record AdminActionLogDto(
        Guid Id,
        string AdminUsername,
        string Action,
        string EntityName,
        string? EntityId,
        string? Details,
        DateTime Timestamp
    );
}
