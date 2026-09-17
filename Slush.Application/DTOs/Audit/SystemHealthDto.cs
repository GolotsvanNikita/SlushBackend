using System;

namespace Slush.Application.DTOs.Audit
{
    public record SystemHealthDto(
        string DatabaseStatus,
        string ExternalApiStatus,
        string OverallStatus,
        DateTime Timestamp
    );
}