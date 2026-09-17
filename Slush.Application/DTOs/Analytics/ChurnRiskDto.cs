using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.DTOs.Analytics
{
    public record ChurnRiskDto(Guid UserId, string Username, string Email, int DaysInactive);
}
