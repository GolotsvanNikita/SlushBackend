using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.DTOs.Monitoring
{
    public record HeatmapPointDto(int DayOfWeek, int Hour, double Intensity);
}
