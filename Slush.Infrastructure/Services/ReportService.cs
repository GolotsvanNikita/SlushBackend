using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Slush.Application.DTOs.Analytics;
using Slush.Application.Interfaces;
using Slush.Infrastructure.Data;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Slush.Infrastructure.Services
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _db;

        public ReportService(AppDbContext db)
        {
            _db = db;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GenerateReportAsync(ExportReportRequestDto request)
        {
            var newUsersCount = await _db.Users.CountAsync(u => u.CreatedAt >= request.StartDate && u.CreatedAt <= request.EndDate);
            var activeLoginsCount = await _db.UserLoginHistories.CountAsync(h => h.LoginTimestamp >= request.StartDate && h.LoginTimestamp <= request.EndDate);

            if (request.Format == ReportFormat.Excel)
            {
                return GenerateExcel(request, newUsersCount, activeLoginsCount);
            }

            return GeneratePdf(request, newUsersCount, activeLoginsCount);
        }

        private byte[] GenerateExcel(ExportReportRequestDto request, int newUsersCount, int activeLoginsCount)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Analytics Report");

            worksheet.Cell(1, 1).Value = "Report Type";
            worksheet.Cell(1, 2).Value = request.ReportType.ToString();

            worksheet.Cell(2, 1).Value = "Period";
            worksheet.Cell(2, 2).Value = $"{request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}";

            worksheet.Cell(4, 1).Value = "Metric";
            worksheet.Cell(4, 2).Value = "Value";

            worksheet.Cell(5, 1).Value = "New Registrations";
            worksheet.Cell(5, 2).Value = newUsersCount;

            worksheet.Cell(6, 1).Value = "Total Logins";
            worksheet.Cell(6, 2).Value = activeLoginsCount;

            worksheet.Range("A1:A6").Style.Font.Bold = true;
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private byte[] GeneratePdf(ExportReportRequestDto request, int newUsersCount, int activeLoginsCount)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Text("Slush Analytics Report")
                        .SemiBold().FontSize(24).FontColor(Colors.Blue.Darken2);

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(x =>
                    {
                        x.Item().Text($"Report Type: {request.ReportType}").FontSize(14);
                        x.Item().Text($"Period: {request.StartDate:d} - {request.EndDate:d}").FontSize(14);
                        x.Item().PaddingTop(20);

                        x.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().BorderBottom(1).Padding(5).Text("Metric").SemiBold();
                                header.Cell().BorderBottom(1).Padding(5).Text("Value").SemiBold();
                            });

                            table.Cell().Padding(5).Text("New Registrations");
                            table.Cell().Padding(5).Text(newUsersCount.ToString());

                            table.Cell().Padding(5).Text("Total Logins");
                            table.Cell().Padding(5).Text(activeLoginsCount.ToString());
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}