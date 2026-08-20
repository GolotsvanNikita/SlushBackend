using System;
using System.Collections.Generic;
using System.Text;

namespace Slush.Application.DTOs.Monitoring
{
    public record GeoPoint(double Lat, double Lng, string City, string Country);
}
