using Restaurant.Application.DTOs.Reports;
using Restaurant.Infrastructure.ExternalServices;

namespace Restaurant.Application.Interfaces
{
    public interface IReportingService
    {
        Task SendReportEmailAsync();
        Task<ReportResponse> GetReportsAsync(ReportRequest request);
        
        Task<byte[]> DownloadReportAsync(ReportDownloadRequest request);
    }
}
