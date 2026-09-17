using Slush.Application.DTOs.Analytics;
using System.Threading.Tasks;

namespace Slush.Application.Interfaces
{
    public interface IReportService
    {
        Task<byte[]> GenerateReportAsync(ExportReportRequestDto request);
    }
}