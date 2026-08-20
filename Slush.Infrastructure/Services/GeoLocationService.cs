using MaxMind.GeoIP2;
using Slush.Application.DTOs.Monitoring;

namespace Slush.Infrastructure.Services;

public interface IGeoLocationService
{
    GeoPoint GetLocation(string? ipAddress);
}

public class GeoLocationService : IGeoLocationService
{
    public GeoPoint GetLocation(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1" || ipAddress == "127.0.0.1")
        {
            return new GeoPoint(46.4825, 30.7233, "Odesa", "UA");
        }

        try
        {
            using var reader = new DatabaseReader("GeoLite2-City.mmdb");
            var response = reader.City(ipAddress);

            return new GeoPoint(
                response.Location.Latitude ?? 0,
                response.Location.Longitude ?? 0,
                response.City.Name ?? "Unknown",
                response.Country.IsoCode ?? "Unknown"
            );
        }
        catch
        {
            return new GeoPoint(0, 0, "Unknown", "Unknown");
        }
    }
}