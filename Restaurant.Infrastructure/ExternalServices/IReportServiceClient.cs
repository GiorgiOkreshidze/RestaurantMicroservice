
namespace Restaurant.Infrastructure.ExternalServices
{
    public interface IReportServiceClient
    {
        Task<HttpResponseMessage> SendReportEmailAsync(string baseUrl);
        
        Task<IEnumerable<ReportResponse>> GetReportsAsync(string baseUrl, string startDate, string endDate, string? locationId);
        
        Task<byte[]> DownloadReportAsync(string baseUrl, string startDate, string endDate, string? locationId, string format);
    }
}
