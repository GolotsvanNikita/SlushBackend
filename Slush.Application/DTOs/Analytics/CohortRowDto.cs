using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.DTOs.Analytics
{
    public record CohortRowDto(string Month, int TotalUsers, List<double> RetentionByMonth);
}
