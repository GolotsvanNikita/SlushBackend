using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.DTOs.Monitoring
{
    public record ActivityPointDto(DateTime Timestamp, int ActiveCount);
}
