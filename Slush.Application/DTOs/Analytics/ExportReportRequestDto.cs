using System;
using System.Text.Json.Serialization;

namespace Slush.Application.DTOs.Analytics
{
    public enum ReportFormat { Pdf, Excel }
    public enum ReportType { RetentionSummary, ActivityReport }

    public record ExportReportRequestDto(
        [property: JsonConverter(typeof(JsonStringEnumConverter))] ReportType ReportType,
        [property: JsonConverter(typeof(JsonStringEnumConverter))] ReportFormat Format,
        DateTime StartDate,
        DateTime EndDate
    );
}